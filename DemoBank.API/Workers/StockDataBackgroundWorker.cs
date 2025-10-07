using DemoBank.API.Fetchers;
using DemoBank.Core.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace DemoBank.API.Workers;

public class StockDataBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StockDataBackgroundWorker> _logger;
    private readonly StockDataFetcher _dataFetcher;

    // Configuration
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _errorRetryInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(6);

    // Stock symbols to track - can be expanded over time
    private readonly ConcurrentQueue<string> _stockQueue = new();
    private readonly HashSet<string> _allStockSymbols = new()
    {
        // Tech giants
        "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA", "META", "TSLA",
        // Financial
        "JPM", "BAC", "WFC", "GS", "MS", "V", "MA",
        // Healthcare
        "JNJ", "UNH", "PFE", "ABBV", "TMO", "MRK", "LLY",
        // Consumer
        "WMT", "PG", "KO", "PEP", "MCD", "NKE", "SBUX",
        // Industrial
        "BA", "CAT", "GE", "MMM", "UPS", "RTX", "LMT",
        // Energy
        "XOM", "CVX", "COP", "SLB", "EOG",
        // Other notable
        "BRK.B", "DIS", "NFLX", "ADBE", "CRM", "ORCL", "IBM"
    };

    // Track last update times for intelligent refresh
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();

    // Statistics
    private int _successfulFetches = 0;
    private int _failedFetches = 0;
    private DateTime _lastFullUpdateTime = DateTime.MinValue;

    public StockDataBackgroundWorker(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<StockDataBackgroundWorker> logger,
        StockDataFetcher dataFetcher)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
        _dataFetcher = dataFetcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Data Background Worker started");

        // Initial delay to let the application start properly
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        // Start with high-priority stocks first
        await InitializeHighPriorityStocks(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshStockData(stoppingToken);

                // Calculate next update interval based on market hours
                var nextUpdateDelay = CalculateNextUpdateDelay();
                _logger.LogInformation($"Next stock data refresh in {nextUpdateDelay.TotalMinutes:F1} minutes");

                await Task.Delay(nextUpdateDelay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in stock data background worker");
                await Task.Delay(_errorRetryInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Stock Data Background Worker stopped");
    }

    private async Task InitializeHighPriorityStocks(CancellationToken cancellationToken)
    {
        var priorityStocks = new[] { "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA", "META", "TSLA" };

        _logger.LogInformation("Initializing high-priority stocks cache");

        foreach (var symbol in priorityStocks)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await FetchAndCacheStock(symbol, cancellationToken);
        }

        // Cache the popular stocks list immediately
        await UpdatePopularStocksCache(cancellationToken);
    }

    private async Task RefreshStockData(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting stock data refresh cycle");
        var startTime = DateTime.UtcNow;

        // Prioritize stocks that haven't been updated recently
        var stocksToUpdate = GetStocksToUpdate();

        foreach (var symbol in stocksToUpdate)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await FetchAndCacheStock(symbol, cancellationToken);
        }

        // Update the popular stocks cache
        await UpdatePopularStocksCache(cancellationToken);

        _lastFullUpdateTime = DateTime.UtcNow;
        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            $"Stock refresh completed. Duration: {duration.TotalSeconds:F1}s, " +
            $"Success: {_successfulFetches}, Failed: {_failedFetches}");
    }

    private async Task FetchAndCacheStock(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var stock = await _dataFetcher.FetchStockData(symbol, cancellationToken);

            if (stock != null)
            {
                var cacheKey = $"stock_{symbol.ToUpper()}";
                _cache.Set(cacheKey, stock, _cacheExpiration);
                _lastUpdateTimes[symbol] = DateTime.UtcNow;
                _successfulFetches++;

                _logger.LogDebug($"Successfully cached stock data for {symbol}");
            }
            else
            {
                _failedFetches++;
                _logger.LogWarning($"Failed to fetch data for {symbol}");
            }
        }
        catch (Exception ex)
        {
            _failedFetches++;
            _logger.LogError(ex, $"Error fetching stock {symbol}");
        }
    }

    private async Task UpdatePopularStocksCache(CancellationToken cancellationToken)
    {
        var popularSymbols = new[]
        {
            "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA",
            "META", "TSLA", "BRK.B", "V", "JPM",
            "WMT", "MA", "JNJ", "PG", "UNH"
        };

        var stocks = new List<StockDto>();

        foreach (var symbol in popularSymbols)
        {
            var cacheKey = $"stock_{symbol.ToUpper()}";
            if (_cache.TryGetValue(cacheKey, out StockDto cachedStock))
            {
                stocks.Add(cachedStock);
            }
        }

        if (stocks.Count > 0)
        {
            _cache.Set("popular_stocks", stocks, _cacheExpiration);
            _logger.LogInformation($"Updated popular stocks cache with {stocks.Count} stocks");
        }
    }

    private List<string> GetStocksToUpdate()
    {
        var now = DateTime.UtcNow;
        var stocksToUpdate = new List<string>();

        // Prioritize stocks that haven't been updated in the last hour
        foreach (var symbol in _allStockSymbols)
        {
            if (!_lastUpdateTimes.TryGetValue(symbol, out var lastUpdate) ||
                (now - lastUpdate) > TimeSpan.FromHours(1))
            {
                stocksToUpdate.Add(symbol);
            }
        }

        // If all stocks are relatively fresh, update the oldest ones
        if (stocksToUpdate.Count == 0)
        {
            stocksToUpdate = _lastUpdateTimes
                .OrderBy(kvp => kvp.Value)
                .Take(10)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        return stocksToUpdate;
    }

    private TimeSpan CalculateNextUpdateDelay()
    {
        var now = DateTime.UtcNow;
        var easternTime = TimeZoneInfo.ConvertTimeFromUtc(now,
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

        // Market hours: 9:30 AM - 4:00 PM ET, Monday-Friday
        bool isWeekday = easternTime.DayOfWeek >= DayOfWeek.Monday &&
                        easternTime.DayOfWeek <= DayOfWeek.Friday;

        bool isMarketHours = isWeekday &&
                            easternTime.TimeOfDay >= TimeSpan.FromHours(9.5) &&
                            easternTime.TimeOfDay <= TimeSpan.FromHours(16);

        if (isMarketHours)
        {
            // During market hours, update more frequently
            return TimeSpan.FromMinutes(15);
        }
        else if (isWeekday && easternTime.TimeOfDay < TimeSpan.FromHours(9.5))
        {
            // Pre-market on weekday, prepare for market open
            return TimeSpan.FromMinutes(30);
        }
        else
        {
            // After hours or weekend, update less frequently
            return TimeSpan.FromHours(2);
        }
    }

    public void AddStockSymbol(string symbol)
    {
        if (_allStockSymbols.Add(symbol.ToUpper()))
        {
            _stockQueue.Enqueue(symbol.ToUpper());
            _logger.LogInformation($"Added new stock symbol to tracking: {symbol}");
        }
    }

    public StockWorkerStatus GetStatus()
    {
        return new StockWorkerStatus
        {
            TotalStocks = _allStockSymbols.Count,
            CachedStocks = _lastUpdateTimes.Count,
            LastFullUpdate = _lastFullUpdateTime,
            SuccessfulFetches = _successfulFetches,
            FailedFetches = _failedFetches,
            NextUpdateIn = CalculateNextUpdateDelay()
        };
    }
}

public class StockWorkerStatus
{
    public int TotalStocks { get; set; }
    public int CachedStocks { get; set; }
    public DateTime LastFullUpdate { get; set; }
    public int SuccessfulFetches { get; set; }
    public int FailedFetches { get; set; }
    public TimeSpan NextUpdateIn { get; set; }
}