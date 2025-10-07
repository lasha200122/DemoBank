using DemoBank.Core.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using System.Text.Json;
using DemoBank.API.Fetchers;
using DemoBank.API.Workers;

namespace DemoBank.API.Services;

public class StockService : IStockService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<StockService> _logger;
    private readonly StockDataFetcher _dataFetcher;
    private readonly StockDataBackgroundWorker _backgroundWorker;

    // Popular stocks that are always prioritized
    private readonly List<string> _popularSymbols = new()
    {
        "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA",
        "META", "TSLA", "BRK.B", "V", "JPM",
        "WMT", "MA", "JNJ", "PG", "UNH"
    };

    public StockService(
        IMemoryCache cache,
        ILogger<StockService> logger,
        StockDataFetcher dataFetcher,
        StockDataBackgroundWorker backgroundWorker)
    {
        _cache = cache;
        _logger = logger;
        _dataFetcher = dataFetcher;
        _backgroundWorker = backgroundWorker;
    }

    public async Task<List<StockDto>> GetPopularStocksAsync()
    {
        // First, try to get from cache (populated by background worker)
        if (_cache.TryGetValue("popular_stocks", out List<StockDto> cachedStocks))
        {
            _logger.LogDebug("Returning popular stocks from cache");
            return cachedStocks;
        }

        // If cache miss, get individual stocks from cache
        _logger.LogInformation("Popular stocks cache miss, fetching from individual caches");
        var stocks = new List<StockDto>();

        foreach (var symbol in _popularSymbols)
        {
            var cacheKey = $"stock_{symbol.ToUpper()}";
            if (_cache.TryGetValue(cacheKey, out StockDto stock))
            {
                stocks.Add(stock);
            }
        }

        // If we have some stocks, cache and return them
        if (stocks.Count > 0)
        {
            _cache.Set("popular_stocks", stocks, TimeSpan.FromMinutes(5));
            _logger.LogInformation($"Cached {stocks.Count} popular stocks for 5 minutes");
            return stocks;
        }

        // If no data in cache at all, return empty list or minimal data
        // The background worker will populate the cache soon
        _logger.LogWarning("No stock data available in cache. Background worker may still be initializing.");
        return new List<StockDto>();
    }

    public async Task<StockDto> GetStockBySymbolAsync(string symbol)
    {
        var cacheKey = $"stock_{symbol.ToUpper()}";

        // First, check cache (populated by background worker)
        if (_cache.TryGetValue(cacheKey, out StockDto cachedStock))
        {
            _logger.LogDebug($"Returning {symbol} from cache");
            return cachedStock;
        }

        _logger.LogInformation($"Cache miss for {symbol}");

        // If it's a new symbol not being tracked, add it to the background worker
        _backgroundWorker.AddStockSymbol(symbol);

        // For immediate response, try to fetch it directly (respecting rate limits)
        try
        {
            _logger.LogInformation($"Fetching {symbol} directly for immediate response");
            var stock = await _dataFetcher.FetchStockData(symbol);

            if (stock != null)
            {
                // Cache it temporarily until background worker refreshes it
                _cache.Set(cacheKey, stock, TimeSpan.FromHours(1));
                return stock;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching stock {symbol} directly");
        }

        return null;
    }

    public async Task<List<StockDto>> SearchStocksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
        {
            return new List<StockDto>();
        }

        var cacheKey = $"search_{query.ToUpper()}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out List<StockDto> cachedResults))
        {
            _logger.LogDebug($"Returning search results for '{query}' from cache");
            return cachedResults;
        }

        try
        {
            // Use the data fetcher for search (it handles rate limiting)
            _logger.LogInformation($"Performing search for '{query}'");
            var stocks = await _dataFetcher.SearchStocks(query, 10);

            if (stocks.Count > 0)
            {
                // Cache search results for 30 minutes
                _cache.Set(cacheKey, stocks, TimeSpan.FromMinutes(30));

                // Add any new symbols to the background worker for tracking
                foreach (var stock in stocks)
                {
                    _backgroundWorker.AddStockSymbol(stock.Symbol);
                }
            }

            return stocks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching stocks for query '{query}'");
            return new List<StockDto>();
        }
    }

    public async Task<StockServiceStatus> GetServiceStatusAsync()
    {
        var workerStatus = _backgroundWorker.GetStatus();
        var rateLimitStatus = _dataFetcher.GetRateLimitStatus();

        return new StockServiceStatus
        {
            WorkerStatus = workerStatus,
            RateLimitStatus = rateLimitStatus,
            CacheStatistics = GetCacheStatistics()
        };
    }

    public async Task<bool> AddStockToTrackingAsync(string symbol)
    {
        try
        {
            _backgroundWorker.AddStockSymbol(symbol);
            _logger.LogInformation($"Added {symbol} to tracking");

            // Try to fetch it immediately for caching
            var stock = await _dataFetcher.FetchStockData(symbol);
            if (stock != null)
            {
                var cacheKey = $"stock_{symbol.ToUpper()}";
                _cache.Set(cacheKey, stock, TimeSpan.FromHours(1));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding {symbol} to tracking");
            return false;
        }
    }

    private CacheStatistics GetCacheStatistics()
    {
        var stats = new CacheStatistics();

        // Count cached stocks
        var stockSymbols = new HashSet<string>();

        // Check popular symbols
        foreach (var symbol in _popularSymbols)
        {
            var cacheKey = $"stock_{symbol.ToUpper()}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                stockSymbols.Add(symbol);
            }
        }

        stats.CachedStockCount = stockSymbols.Count;
        stats.HasPopularStocksCache = _cache.TryGetValue("popular_stocks", out _);

        return stats;
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