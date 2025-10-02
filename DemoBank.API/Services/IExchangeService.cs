using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface IExchangeService
{
    Task<ExchangeResultDto> ExchangeCurrencyAsync(Guid userId, ExchangeRequestDto exchangeDto);
    Task<ExchangeQuoteDto> GetExchangeQuoteAsync(string fromCurrency, string toCurrency, decimal amount);
    Task<List<ExchangeRateHistoryDto>> GetRateHistoryAsync(string fromCurrency, string toCurrency, int days = 30);
    Task<List<CurrencyPairDto>> GetFavoritePairsAsync(Guid userId);
    Task<bool> AddFavoritePairAsync(Guid userId, string fromCurrency, string toCurrency);
    Task<bool> RemoveFavoritePairAsync(Guid userId, Guid pairId);
    Task<List<RateAlertDto>> GetRateAlertsAsync(Guid userId);
    Task<RateAlertDto> CreateRateAlertAsync(Guid userId, CreateRateAlertDto alertDto);
    Task<bool> DeleteRateAlertAsync(Guid userId, Guid alertId);
    Task<List<ExchangeTransactionDto>> GetExchangeHistoryAsync(Guid userId, int limit = 50);
    Task<CurrencyTrendsDto> GetCurrencyTrendsAsync(string currency, int days = 7);
    Task<List<PopularCurrencyPairDto>> GetPopularPairsAsync();
    Task<ExchangeFeeDto> CalculateExchangeFeeAsync(decimal amount, string fromCurrency, string toCurrency);
};