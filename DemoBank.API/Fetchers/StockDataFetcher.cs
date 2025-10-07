using DemoBank.Core.DTOs;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DemoBank.API.Fetchers;

public class StockDataFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockDataFetcher> _logger;
    private readonly string _apiKey = "FByWSg7O5bgeRPbk27vRS0mq1jbexhQM";

    // Enhanced rate limiting with sliding window
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lockObject = new();

    // Rate limit configuration
    private const int MaxRequestsPerMinute = 5;
    private const int MaxRequestsPer10Seconds = 2; // Additional short-term limit

    // Request tracking for analytics
    private long _totalRequests = 0;
    private long _successfulRequests = 0;
    private long _rateLimitHits = 0;

    public StockDataFetcher(ILogger<StockDataFetcher> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;
    }

    public async Task<StockDto?> FetchStockData(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply rate limiting
            await WaitForRateLimit(cancellationToken);

            // Get ticker details
            var detailsUrl = $"https://api.polygon.io/v3/reference/tickers/{symbol.ToUpper()}?apiKey={_apiKey}";
            var detailsResponse = await _httpClient.GetAsync(detailsUrl, cancellationToken);

            if (!detailsResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to fetch ticker details for {symbol}: {detailsResponse.StatusCode}");
                return null;
            }

            var detailsJson = await detailsResponse.Content.ReadAsStringAsync(cancellationToken);
            var detailsData = JsonSerializer.Deserialize<PolygonTickerResponse>(detailsJson);

            if (detailsData?.results == null)
            {
                _logger.LogWarning($"No ticker details found for {symbol}");
                return null;
            }

            // Apply rate limiting for second request
            await WaitForRateLimit(cancellationToken);

            // Get price data
            var priceUrl = $"https://api.polygon.io/v2/aggs/ticker/{symbol.ToUpper()}/prev?adjusted=true&apiKey={_apiKey}";
            var priceResponse = await _httpClient.GetAsync(priceUrl, cancellationToken);

            decimal? price = null;
            if (priceResponse.IsSuccessStatusCode)
            {
                var priceJson = await priceResponse.Content.ReadAsStringAsync(cancellationToken);
                var priceData = JsonSerializer.Deserialize<PolygonPriceResponse>(priceJson);
                price = priceData?.results?.FirstOrDefault()?.c;
            }

            Interlocked.Increment(ref _successfulRequests);

            return new StockDto
            {
                Symbol = symbol.ToUpper(),
                Name = detailsData.results.name,
                LogoUrl = detailsData.results.branding?.logo_url ?? detailsData.results.branding?.icon_url,
                Price = price,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Network error fetching stock data for {symbol}");
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning($"Request timeout or cancelled for {symbol}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error fetching stock data for {symbol}");
            return null;
        }
    }

    public async Task<List<StockDto>> SearchStocks(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            await WaitForRateLimit(cancellationToken);

            var searchUrl = $"https://api.polygon.io/v3/reference/tickers?search={query}&active=true&limit={limit}&apiKey={_apiKey}";
            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Search failed for query '{query}': {response.StatusCode}");
                return new List<StockDto>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<PolygonSearchResponse>(json);

            if (data?.results == null || !data.results.Any())
            {
                return new List<StockDto>();
            }

            var stocks = new List<StockDto>();

            // Fetch limited price data for search results (respecting rate limits)
            foreach (var result in data.results.Take(Math.Min(limit, 3))) // Limit to 3 to avoid rate limit
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await WaitForRateLimit(cancellationToken);

                try
                {
                    var priceUrl = $"https://api.polygon.io/v2/aggs/ticker/{result.ticker}/prev?adjusted=true&apiKey={_apiKey}";
                    var priceResponse = await _httpClient.GetAsync(priceUrl, cancellationToken);

                    decimal? price = null;
                    if (priceResponse.IsSuccessStatusCode)
                    {
                        var priceJson = await priceResponse.Content.ReadAsStringAsync(cancellationToken);
                        var priceData = JsonSerializer.Deserialize<PolygonPriceResponse>(priceJson);
                        price = priceData?.results?.FirstOrDefault()?.c;
                    }

                    stocks.Add(new StockDto
                    {
                        Symbol = result.ticker,
                        Name = result.name,
                        LogoUrl = result.branding?.logo_url ?? result.branding?.icon_url,
                        Price = price,
                        LastUpdated = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not fetch price for {result.ticker}");
                    stocks.Add(new StockDto
                    {
                        Symbol = result.ticker,
                        Name = result.name,
                        LogoUrl = result.branding?.logo_url ?? result.branding?.icon_url,
                        Price = null,
                        LastUpdated = DateTime.UtcNow
                    });
                }
            }

            Interlocked.Increment(ref _successfulRequests);
            return stocks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching stocks for query '{query}'");
            return new List<StockDto>();
        }
    }

    private async Task WaitForRateLimit(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                Interlocked.Increment(ref _totalRequests);

                // Clean up old request times (older than 1 minute)
                while (_requestTimes.Count > 0 && (now - _requestTimes.Peek()).TotalMinutes >= 1)
                {
                    _requestTimes.Dequeue();
                }

                // Check rate limit (5 per minute)
                if (_requestTimes.Count >= MaxRequestsPerMinute)
                {
                    var oldestRequest = _requestTimes.Peek();
                    var waitTime = TimeSpan.FromMinutes(1) - (now - oldestRequest);

                    if (waitTime.TotalMilliseconds > 0)
                    {
                        Interlocked.Increment(ref _rateLimitHits);
                        _logger.LogDebug($"Rate limit reached. Waiting {waitTime.TotalSeconds:F1} seconds");

                        // Use async delay instead of Thread.Sleep
                        Task.Delay(waitTime, cancellationToken).Wait(cancellationToken);
                    }

                    _requestTimes.Dequeue();
                }

                // Additional check for burst protection (2 requests per 10 seconds)
                var recentRequests = _requestTimes.Where(t => (now - t).TotalSeconds < 10).Count();
                if (recentRequests >= MaxRequestsPer10Seconds)
                {
                    var waitTime = TimeSpan.FromSeconds(10) - (now - _requestTimes.Last(t => (now - t).TotalSeconds < 10));
                    if (waitTime.TotalMilliseconds > 0)
                    {
                        _logger.LogDebug($"Burst protection: waiting {waitTime.TotalSeconds:F1} seconds");
                        Task.Delay(waitTime, cancellationToken).Wait(cancellationToken);
                    }
                }

                _requestTimes.Enqueue(now);
            }
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public RateLimitStatus GetRateLimitStatus()
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            var recentRequests = _requestTimes.Where(t => (now - t).TotalMinutes < 1).Count();

            return new RateLimitStatus
            {
                RequestsInLastMinute = recentRequests,
                RemainingRequests = Math.Max(0, MaxRequestsPerMinute - recentRequests),
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                RateLimitHits = _rateLimitHits,
                NextResetTime = recentRequests > 0
                    ? _requestTimes.First().AddMinutes(1)
                    : DateTime.UtcNow
            };
        }
    }
}

public class RateLimitStatus
{
    public int RequestsInLastMinute { get; set; }
    public int RemainingRequests { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long RateLimitHits { get; set; }
    public DateTime NextResetTime { get; set; }
}

// DTOs for Polygon API responses
public class PolygonTickerResponse
{
    [JsonPropertyName("results")]
    public TickerResult results { get; set; }
}

public class TickerResult
{
    [JsonPropertyName("ticker")]
    public string ticker { get; set; }

    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("branding")]
    public BrandingInfo branding { get; set; }
}

public class BrandingInfo
{
    [JsonPropertyName("logo_url")]
    public string logo_url { get; set; }

    [JsonPropertyName("icon_url")]
    public string icon_url { get; set; }
}

public class PolygonPriceResponse
{
    [JsonPropertyName("ticker")]
    public string ticker { get; set; }

    [JsonPropertyName("results")]
    public List<PriceResult> results { get; set; }
}

public class PriceResult
{
    [JsonPropertyName("c")]
    public decimal c { get; set; } // close price
}

public class PolygonSearchResponse
{
    [JsonPropertyName("results")]
    public List<SearchResult> results { get; set; }
}

public class SearchResult
{
    [JsonPropertyName("ticker")]
    public string ticker { get; set; }

    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("branding")]
    public BrandingInfo branding { get; set; }
}