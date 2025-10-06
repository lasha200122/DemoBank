using DemoBank.Core.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DemoBank.API.Services;

public class StockService : IStockService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StockService> _logger;
    private readonly string _apiKey = "FByWSg7O5bgeRPbk27vRS0mq1jbexhQM";

    // Popular stocks to display
    private readonly List<string> _popularSymbols = new List<string>
    {
        "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA",
        "META", "TSLA", "BRK.B", "V", "JPM",
        "WMT", "MA", "JNJ", "PG", "UNH"
    };

    // Rate limiting
    private readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1, 1);
    private readonly Queue<DateTime> _requestTimes = new Queue<DateTime>();
    private readonly object _lockObject = new object();

    public StockService(IMemoryCache cache, ILogger<StockService> logger)
    {
        _httpClient = new HttpClient();
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<StockDto>> GetPopularStocksAsync()
    {
        var cacheKey = "popular_stocks";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out List<StockDto> cachedStocks))
        {
            _logger.LogInformation("Returning popular stocks from cache");
            return cachedStocks;
        }

        _logger.LogInformation("Fetching popular stocks from Polygon API");
        var stocks = new List<StockDto>();

        foreach (var symbol in _popularSymbols)
        {
            try
            {
                var stock = await GetStockBySymbolAsync(symbol);
                if (stock != null)
                {
                    stocks.Add(stock);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching stock {symbol}");
            }
        }

        // Cache for 6 hours
        _cache.Set(cacheKey, stocks, TimeSpan.FromHours(6));
        _logger.LogInformation($"Cached {stocks.Count} popular stocks for 6 hours");

        return stocks;
    }

    public async Task<StockDto> GetStockBySymbolAsync(string symbol)
    {
        var cacheKey = $"stock_{symbol.ToUpper()}";

        // Check cache
        if (_cache.TryGetValue(cacheKey, out StockDto cachedStock))
        {
            _logger.LogInformation($"Returning {symbol} from cache");
            return cachedStock;
        }

        // Rate limiting
        await WaitForRateLimit();

        try
        {
            // Get ticker details (company name and logo)
            var detailsUrl = $"https://api.polygon.io/v3/reference/tickers/{symbol.ToUpper()}?apiKey={_apiKey}";
            var detailsResponse = await _httpClient.GetStringAsync(detailsUrl);
            var detailsData = JsonSerializer.Deserialize<PolygonTickerResponse>(detailsResponse);

            if (detailsData?.results == null)
            {
                _logger.LogWarning($"No ticker details found for {symbol}");
                return null;
            }

            // Get previous day's price
            await WaitForRateLimit();
            var priceUrl = $"https://api.polygon.io/v2/aggs/ticker/{symbol.ToUpper()}/prev?adjusted=true&apiKey={_apiKey}";
            var priceResponse = await _httpClient.GetStringAsync(priceUrl);
            var priceData = JsonSerializer.Deserialize<PolygonPriceResponse>(priceResponse);

            var stock = new StockDto
            {
                Symbol = symbol.ToUpper(),
                Name = detailsData.results.name,
                LogoUrl = detailsData.results.branding?.logo_url ?? detailsData.results.branding?.icon_url,
                Price = priceData?.results?.FirstOrDefault()?.c,
                LastUpdated = DateTime.UtcNow
            };

            // Cache for 6 hours
            _cache.Set(cacheKey, stock, TimeSpan.FromHours(6));
            _logger.LogInformation($"Cached stock data for {symbol}");

            return stock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching stock data for {symbol}");
            return null;
        }
    }

    public async Task<List<StockDto>> SearchStocksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
        {
            return new List<StockDto>();
        }

        var cacheKey = $"search_{query.ToUpper()}";

        if (_cache.TryGetValue(cacheKey, out List<StockDto> cachedResults))
        {
            _logger.LogInformation($"Returning search results for '{query}' from cache");
            return cachedResults;
        }

        await WaitForRateLimit();

        try
        {
            var searchUrl = $"https://api.polygon.io/v3/reference/tickers?search={query}&active=true&limit=10&apiKey={_apiKey}";
            var response = await _httpClient.GetStringAsync(searchUrl);
            var data = JsonSerializer.Deserialize<PolygonSearchResponse>(response);

            if (data?.results == null || !data.results.Any())
            {
                return new List<StockDto>();
            }

            var stocks = new List<StockDto>();
            foreach (var result in data.results.Take(10))
            {
                // Get price for each result
                await WaitForRateLimit();
                var priceUrl = $"https://api.polygon.io/v2/aggs/ticker/{result.ticker}/prev?adjusted=true&apiKey={_apiKey}";

                try
                {
                    var priceResponse = await _httpClient.GetStringAsync(priceUrl);
                    var priceData = JsonSerializer.Deserialize<PolygonPriceResponse>(priceResponse);

                    stocks.Add(new StockDto
                    {
                        Symbol = result.ticker,
                        Name = result.name,
                        LogoUrl = result.branding?.logo_url ?? result.branding?.icon_url,
                        Price = priceData?.results?.FirstOrDefault()?.c,
                        LastUpdated = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not fetch price for {result.ticker}");
                    // Add without price
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

            // Cache for 1 hour
            _cache.Set(cacheKey, stocks, TimeSpan.FromHours(1));
            _logger.LogInformation($"Cached search results for '{query}'");

            return stocks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching stocks for query '{query}'");
            return new List<StockDto>();
        }
    }

    // Rate limiter: 5 requests per minute
    private async Task WaitForRateLimit()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                // Remove requests older than 1 minute
                while (_requestTimes.Count > 0 && (now - _requestTimes.Peek()).TotalMinutes >= 1)
                {
                    _requestTimes.Dequeue();
                }

                // If we've made 5 requests in the last minute, wait
                if (_requestTimes.Count >= 5)
                {
                    var oldestRequest = _requestTimes.Peek();
                    var waitTime = TimeSpan.FromMinutes(1) - (now - oldestRequest);
                    if (waitTime.TotalMilliseconds > 0)
                    {
                        Thread.Sleep(waitTime);
                    }
                    _requestTimes.Dequeue();
                }

                _requestTimes.Enqueue(DateTime.UtcNow);
            }
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}

// Polygon API Response Models
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