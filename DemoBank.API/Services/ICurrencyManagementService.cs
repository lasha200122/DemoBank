using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DemoBank.API.Services;

public interface ICurrencyManagementService
{
    // Admin operations
    Task<CurrencyDetailsDto> CreateCurrencyAsync(CreateCurrencyDto dto, string createdBy);
    Task<CurrencyDetailsDto> UpdateCurrencyAsync(string code, UpdateCurrencyDto dto, string updatedBy);
    Task<bool> DeleteCurrencyAsync(string code);
    Task<bool> ToggleCurrencyStatusAsync(string code, bool isActive);

    // Currency queries
    Task<List<CurrencyDetailsDto>> GetAllCurrenciesDetailedAsync(bool includeInactive = false);
    Task<CurrencyDetailsDto> GetCurrencyDetailsAsync(string code);
    Task<List<CurrencyDetailsDto>> GetCryptoCurrenciesAsync();
    Task<List<CurrencyDetailsDto>> GetFiatCurrenciesAsync();

    // Price management
    Task<bool> UpdateExchangeRatesAsync(Dictionary<string, decimal> rates);
    Task<bool> UpdateCryptoPricesAsync();
    Task<CurrencyStatisticsDto> GetCurrencyStatisticsAsync(string code);
    Task<CurrencyPriceHistoryDto> GetPriceHistoryAsync(string code, int days = 7);

    // Crypto specific
    Task<CryptoWalletDto> GetOrCreateCryptoWalletAsync(Guid userId, string currencyCode);
    Task<List<CryptoWalletDto>> GetUserCryptoWalletsAsync(Guid userId);
    Task<bool> ProcessCryptoDepositAsync(string walletAddress, decimal amount, string txHash);
    Task<bool> ValidateCryptoAddressAsync(string address, string currency, string network);
}

public class CurrencyManagementService : ICurrencyManagementService
{
    private readonly DemoBankContext _context;
    private readonly IMemoryCache _cache;
    private readonly INotificationHelper _notificationHelper;
    private readonly ILogger<CurrencyManagementService> _logger;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public CurrencyManagementService(
        DemoBankContext context,
        IMemoryCache cache,
        INotificationHelper notificationHelper,
        ILogger<CurrencyManagementService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _cache = cache;
        _notificationHelper = notificationHelper;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<CurrencyDetailsDto> CreateCurrencyAsync(CreateCurrencyDto dto, string createdBy)
    {
        // Check if currency already exists
        var existingCurrency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == dto.Code.ToUpper());

        if (existingCurrency != null)
            throw new InvalidOperationException($"Currency with code {dto.Code} already exists");

        var currency = new Currency
        {
            Code = dto.Code.ToUpper(),
            Name = dto.Name,
            Symbol = dto.Symbol,
            Type = Enum.Parse<CurrencyType>(dto.Type),
            ExchangeRateToUSD = dto.ExchangeRateToUSD,
            ImageUrl = dto.ImageUrl,
            LogoUrl = dto.LogoUrl,
            DecimalPlaces = dto.DecimalPlaces,
            MinimumTransactionAmount = dto.MinimumTransactionAmount,
            MaximumTransactionAmount = dto.MaximumTransactionAmount,
            Network = dto.Network,
            ContractAddress = dto.ContractAddress,
            NetworkFee = dto.NetworkFee,
            ConfirmationsRequired = dto.ConfirmationsRequired,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        _context.Currencies.Add(currency);
        await _context.SaveChangesAsync();

        // Clear cache
        ClearCurrencyCache();

        _logger.LogInformation($"Currency {currency.Code} created by {createdBy}");

        return MapToDetailsDto(currency);
    }

    public async Task<CurrencyDetailsDto> UpdateCurrencyAsync(string code, UpdateCurrencyDto dto, string updatedBy)
    {
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (currency == null)
            throw new InvalidOperationException($"Currency {code} not found");

        // Update only provided fields
        if (!string.IsNullOrEmpty(dto.Name))
            currency.Name = dto.Name;

        if (!string.IsNullOrEmpty(dto.Symbol))
            currency.Symbol = dto.Symbol;

        if (dto.ExchangeRateToUSD.HasValue)
            currency.ExchangeRateToUSD = dto.ExchangeRateToUSD.Value;

        if (!string.IsNullOrEmpty(dto.ImageUrl))
            currency.ImageUrl = dto.ImageUrl;

        if (!string.IsNullOrEmpty(dto.LogoUrl))
            currency.LogoUrl = dto.LogoUrl;

        if (dto.DecimalPlaces.HasValue)
            currency.DecimalPlaces = dto.DecimalPlaces.Value;

        if (dto.MinimumTransactionAmount.HasValue)
            currency.MinimumTransactionAmount = dto.MinimumTransactionAmount.Value;

        if (dto.MaximumTransactionAmount.HasValue)
            currency.MaximumTransactionAmount = dto.MaximumTransactionAmount.Value;

        if (!string.IsNullOrEmpty(dto.Network))
            currency.Network = dto.Network;

        if (!string.IsNullOrEmpty(dto.ContractAddress))
            currency.ContractAddress = dto.ContractAddress;

        if (dto.NetworkFee.HasValue)
            currency.NetworkFee = dto.NetworkFee.Value;

        if (dto.ConfirmationsRequired.HasValue)
            currency.ConfirmationsRequired = dto.ConfirmationsRequired.Value;

        if (dto.IsActive.HasValue)
            currency.IsActive = dto.IsActive.Value;

        currency.LastUpdated = DateTime.UtcNow;
        currency.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        // Clear cache
        ClearCurrencyCache();

        _logger.LogInformation($"Currency {currency.Code} updated by {updatedBy}");

        return MapToDetailsDto(currency);
    }

    public async Task<bool> DeleteCurrencyAsync(string code)
    {
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (currency == null)
            return false;

        // Check if currency is in use
        var hasAccounts = await _context.Accounts
            .AnyAsync(a => a.Currency == currency.Code);

        if (hasAccounts)
            throw new InvalidOperationException($"Cannot delete currency {code} as it is in use by accounts");

        _context.Currencies.Remove(currency);
        await _context.SaveChangesAsync();

        // Clear cache
        ClearCurrencyCache();

        _logger.LogInformation($"Currency {currency.Code} deleted");

        return true;
    }

    public async Task<bool> ToggleCurrencyStatusAsync(string code, bool isActive)
    {
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (currency == null)
            return false;

        currency.IsActive = isActive;
        currency.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Clear cache
        ClearCurrencyCache();

        _logger.LogInformation($"Currency {currency.Code} status changed to {(isActive ? "active" : "inactive")}");

        return true;
    }

    public async Task<List<CurrencyDetailsDto>> GetAllCurrenciesDetailedAsync(bool includeInactive = false)
    {
        var cacheKey = $"all_currencies_detailed_{includeInactive}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = _cacheExpiration;

            var query = _context.Currencies.AsQueryable();

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            var currencies = await query
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Code)
                .ToListAsync();

            return currencies.Select(MapToDetailsDto).ToList();
        });
    }

    public async Task<CurrencyDetailsDto> GetCurrencyDetailsAsync(string code)
    {
        var cacheKey = $"currency_details_{code.ToUpper()}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = _cacheExpiration;

            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

            if (currency == null)
                return null;

            return MapToDetailsDto(currency);
        });
    }

    public async Task<List<CurrencyDetailsDto>> GetCryptoCurrenciesAsync()
    {
        var cacheKey = "crypto_currencies";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = _cacheExpiration;

            var currencies = await _context.Currencies
                .Where(c => c.Type == CurrencyType.Crypto && c.IsActive)
                .OrderBy(c => c.Code)
                .ToListAsync();

            return currencies.Select(MapToDetailsDto).ToList();
        });
    }

    public async Task<List<CurrencyDetailsDto>> GetFiatCurrenciesAsync()
    {
        var cacheKey = "fiat_currencies";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = _cacheExpiration;

            var currencies = await _context.Currencies
                .Where(c => c.Type == CurrencyType.Fiat && c.IsActive)
                .OrderBy(c => c.Code)
                .ToListAsync();

            return currencies.Select(MapToDetailsDto).ToList();
        });
    }

    public async Task<bool> UpdateExchangeRatesAsync(Dictionary<string, decimal> rates)
    {
        foreach (var rate in rates)
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.Code == rate.Key.ToUpper());

            if (currency != null)
            {
                currency.ExchangeRateToUSD = rate.Value;
                currency.LastUpdated = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        // Clear cache
        ClearCurrencyCache();

        return true;
    }

    public async Task<bool> UpdateCryptoPricesAsync()
    {
        try
        {
            // In production, you would call a real crypto price API like CoinGecko or CoinMarketCap
            // This is a simulated implementation

            var cryptoCurrencies = await _context.Currencies
                .Where(c => c.Type == CurrencyType.Crypto && c.IsActive)
                .ToListAsync();

            foreach (var crypto in cryptoCurrencies)
            {
                // Simulate price fluctuation (±5%)
                var random = new Random();
                var changePercent = (random.NextDouble() - 0.5) * 0.1; // -5% to +5%
                crypto.ExchangeRateToUSD *= (decimal)(1 + changePercent);
                crypto.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Clear cache
            ClearCurrencyCache();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating crypto prices");
            return false;
        }
    }

    public async Task<CurrencyStatisticsDto> GetCurrencyStatisticsAsync(string code)
    {
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (currency == null)
            return null;

        // In production, fetch from real API
        // This is simulated data
        var random = new Random();

        return new CurrencyStatisticsDto
        {
            Code = currency.Code,
            Name = currency.Name,
            Type = currency.Type.ToString(),
            CurrentPrice = currency.ExchangeRateToUSD,
            Volume24h = (decimal)(random.Next(1000000, 10000000)),
            High24h = currency.ExchangeRateToUSD * 1.05m,
            Low24h = currency.ExchangeRateToUSD * 0.95m,
            PercentChange1h = (decimal)(random.NextDouble() * 4 - 2),
            PercentChange24h = (decimal)(random.NextDouble() * 10 - 5),
            PercentChange7d = (decimal)(random.NextDouble() * 20 - 10),
            MarketCap = currency.Type == CurrencyType.Crypto ?
                (decimal)(random.Next(1000000000, 1000000000)) : 0,
            CirculatingSupply = currency.Type == CurrencyType.Crypto ?
                (decimal)(random.Next(1000000, 100000000)) : 0,
            TotalSupply = currency.Type == CurrencyType.Crypto ?
                (decimal)(random.Next(1000000, 100000000)) : 0,
            LastUpdated = currency.LastUpdated
        };
    }

    public async Task<CurrencyPriceHistoryDto> GetPriceHistoryAsync(string code, int days = 7)
    {
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

        if (currency == null)
            return null;

        // In production, fetch from database or API
        // This generates simulated historical data
        var history = new List<PricePointDto>();
        var currentPrice = currency.ExchangeRateToUSD;
        var random = new Random();

        for (int i = days; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            var priceVariation = (decimal)(1 + (random.NextDouble() - 0.5) * 0.1);

            history.Add(new PricePointDto
            {
                Timestamp = date,
                Price = currentPrice * priceVariation,
                Volume = (decimal)(random.Next(100000, 1000000))
            });
        }

        return new CurrencyPriceHistoryDto
        {
            Currency = currency.Code,
            PriceHistory = history
        };
    }

    public async Task<CryptoWalletDto> GetOrCreateCryptoWalletAsync(Guid userId, string currencyCode)
    {
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == currencyCode.ToUpper() &&
                                     c.Type == CurrencyType.Crypto &&
                                     c.IsActive);

        if (currency == null)
            throw new InvalidOperationException($"Crypto currency {currencyCode} not found or not active");

        // Check if user already has a wallet for this currency
        // In production, this would be stored in a CryptoWallets table
        var walletAddress = GenerateCryptoAddress(currencyCode, userId);

        return new CryptoWalletDto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrencyCode = currency.Code,
            WalletAddress = walletAddress,
            Network = currency.Network,
            Balance = 0,
            PendingBalance = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RecentTransactions = new List<CryptoTransactionDto>()
        };
    }

    public async Task<List<CryptoWalletDto>> GetUserCryptoWalletsAsync(Guid userId)
    {
        var cryptoCurrencies = await GetCryptoCurrenciesAsync();
        var wallets = new List<CryptoWalletDto>();

        foreach (var currency in cryptoCurrencies)
        {
            // In production, fetch from database
            wallets.Add(new CryptoWalletDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CurrencyCode = currency.Code,
                WalletAddress = GenerateCryptoAddress(currency.Code, userId),
                Network = currency.Network,
                Balance = 0,
                PendingBalance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RecentTransactions = new List<CryptoTransactionDto>()
            });
        }

        return wallets;
    }

    public async Task<bool> ProcessCryptoDepositAsync(string walletAddress, decimal amount, string txHash)
    {
        // In production, this would:
        // 1. Verify the transaction on the blockchain
        // 2. Check confirmations
        // 3. Update user balance
        // 4. Create transaction record

        _logger.LogInformation($"Processing crypto deposit: {amount} to {walletAddress}, TX: {txHash}");

        // Simulate processing
        await Task.Delay(1000);

        return true;
    }

    public async Task<bool> ValidateCryptoAddressAsync(string address, string currency, string network)
    {
        // In production, validate address format based on currency and network
        // This is a simplified validation

        if (string.IsNullOrWhiteSpace(address))
            return false;

        var validationRules = new Dictionary<string, Func<string, bool>>
        {
            ["BTC"] = addr => addr.StartsWith("1") || addr.StartsWith("3") || addr.StartsWith("bc1"),
            ["ETH"] = addr => addr.StartsWith("0x") && addr.Length == 42,
            ["USDT"] = addr => addr.StartsWith("0x") && addr.Length == 42, // ERC20
        };

        if (validationRules.TryGetValue(currency.ToUpper(), out var validator))
        {
            return await Task.FromResult(validator(address));
        }

        // Default validation
        return await Task.FromResult(address.Length >= 20);
    }

    private CurrencyDetailsDto MapToDetailsDto(Currency currency)
    {
        return new CurrencyDetailsDto
        {
            Id = currency.Id,
            Code = currency.Code,
            Name = currency.Name,
            Symbol = currency.Symbol,
            Type = currency.Type.ToString(),
            ExchangeRateToUSD = currency.ExchangeRateToUSD,
            ImageUrl = currency.ImageUrl,
            LogoUrl = currency.LogoUrl,
            DecimalPlaces = currency.DecimalPlaces,
            MinimumTransactionAmount = currency.MinimumTransactionAmount,
            MaximumTransactionAmount = currency.MaximumTransactionAmount,
            IsActive = currency.IsActive,
            IsDefault = currency.IsDefault,
            Network = currency.Network,
            ContractAddress = currency.ContractAddress,
            NetworkFee = currency.NetworkFee,
            ConfirmationsRequired = currency.ConfirmationsRequired,
            LastUpdated = currency.LastUpdated,
            CreatedAt = currency.CreatedAt,
            CreatedBy = currency.CreatedBy,
            UpdatedBy = currency.UpdatedBy,
            // These would be calculated from actual data
            Volume24h = 0,
            PercentChange24h = 0,
            MarketCap = 0
        };
    }

    private string GenerateCryptoAddress(string currency, Guid userId)
    {
        // In production, use proper crypto library to generate addresses
        // This is a simulated address generation
        var hash = System.Security.Cryptography.SHA256.Create()
            .ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{currency}{userId}"));

        var prefix = currency.ToUpper() switch
        {
            "BTC" => "1",
            "ETH" => "0x",
            "USDT" => "0x",
            _ => "0x"
        };

        return prefix + Convert.ToBase64String(hash).Replace("/", "").Replace("+", "").Substring(0, 40);
    }

    private void ClearCurrencyCache()
    {
        _cache.Remove("all_currencies");
        _cache.Remove("all_currencies_detailed_true");
        _cache.Remove("all_currencies_detailed_false");
        _cache.Remove("crypto_currencies");
        _cache.Remove("fiat_currencies");
        _cache.Remove("all_exchange_rates");
    }
}