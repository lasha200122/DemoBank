using DemoBank.API.Data;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DemoBank.API.Services;

public class CurrencyService : ICurrencyService
{
    private readonly DemoBankContext _context;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public CurrencyService(DemoBankContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<Currency>> GetAllCurrenciesAsync()
    {
        return await _cache.GetOrCreateAsync("all_currencies", async entry =>
        {
            entry.SlidingExpiration = _cacheExpiration;
            return await _context.Currencies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Code)
                .ToListAsync();
        });
    }

    public async Task<Currency> GetCurrencyAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        var cacheKey = $"currency_{code.ToUpper()}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = _cacheExpiration;
            return await _context.Currencies
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper() && c.IsActive);
        });
    }

    public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        if (fromCurrency.ToUpper() == toCurrency.ToUpper())
            return 1;

        var from = await GetCurrencyAsync(fromCurrency);
        var to = await GetCurrencyAsync(toCurrency);

        if (from == null || to == null)
            throw new InvalidOperationException($"Currency {fromCurrency} or {toCurrency} not found");

        // All rates are stored relative to USD
        // To convert from A to B: (A to USD) / (B to USD)
        if (toCurrency.ToUpper() == "USD")
            return 1 / from.ExchangeRateToUSD;

        if (fromCurrency.ToUpper() == "USD")
            return to.ExchangeRateToUSD;

        // Convert through USD
        return to.ExchangeRateToUSD / from.ExchangeRateToUSD;
    }

    public async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (amount == 0)
            return 0;

        var rate = await GetExchangeRateAsync(fromCurrency, toCurrency);
        return Math.Round(amount * rate, 2);
    }

    public async Task<bool> UpdateExchangeRateAsync(string currencyCode, decimal newRate)
    {
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == currencyCode.ToUpper());

        if (currency == null)
            return false;

        currency.ExchangeRateToUSD = newRate;
        currency.LastUpdated = DateTime.UtcNow;

        _context.Currencies.Update(currency);
        var result = await _context.SaveChangesAsync() > 0;

        if (result)
        {
            // Clear cache
            _cache.Remove($"currency_{currencyCode.ToUpper()}");
            _cache.Remove("all_currencies");
            _cache.Remove("all_exchange_rates");
        }

        return result;
    }

    public async Task<Dictionary<string, decimal>> GetAllExchangeRatesAsync()
    {
        return await _cache.GetOrCreateAsync("all_exchange_rates", async entry =>
        {
            entry.SlidingExpiration = _cacheExpiration;

            var currencies = await GetAllCurrenciesAsync();
            var rates = new Dictionary<string, decimal>();

            foreach (var currency in currencies)
            {
                // Rate from currency to USD
                rates[$"{currency.Code}_USD"] = 1 / currency.ExchangeRateToUSD;
                // Rate from USD to currency
                rates[$"USD_{currency.Code}"] = currency.ExchangeRateToUSD;
            }

            return rates;
        });
    }
}