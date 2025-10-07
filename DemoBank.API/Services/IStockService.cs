using DemoBank.API.Fetchers;
using DemoBank.API.Workers;
using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface IStockService
{
    Task<List<StockDto>> GetPopularStocksAsync();
    Task<StockDto> GetStockBySymbolAsync(string symbol);
    Task<List<StockDto>> SearchStocksAsync(string query);
    Task<StockServiceStatus> GetServiceStatusAsync();
    Task<bool> AddStockToTrackingAsync(string symbol);
}

public class StockServiceStatus
{
    public StockWorkerStatus WorkerStatus { get; set; }
    public RateLimitStatus RateLimitStatus { get; set; }
    public CacheStatistics CacheStatistics { get; set; }
}

public class CacheStatistics
{
    public int CachedStockCount { get; set; }
    public bool HasPopularStocksCache { get; set; }
}