using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface IStockService
{
    Task<List<StockDto>> GetPopularStocksAsync();
    Task<StockDto> GetStockBySymbolAsync(string symbol);
    Task<List<StockDto>> SearchStocksAsync(string query);
}