using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface ICurrencyService
{
    Task<List<Currency>> GetAllCurrenciesAsync();
    Task<Currency> GetCurrencyAsync(string code);
    Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency);
    Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency);
    Task<bool> UpdateExchangeRateAsync(string currencyCode, decimal newRate);
    Task<Dictionary<string, decimal>> GetAllExchangeRatesAsync();
}