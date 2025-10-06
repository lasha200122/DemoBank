using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class ExchangeService : IExchangeService
{
    private readonly DemoBankContext _context;
    private readonly IAccountService _accountService;
    private readonly ICurrencyService _currencyService;
    private readonly INotificationHelper _notificationHelper;

    // Exchange fee percentage (e.g., 0.5% = 0.005)
    private const decimal EXCHANGE_FEE_RATE = 0.005m;
    private const decimal MIN_EXCHANGE_FEE = 0.50m; // Minimum fee in USD

    public ExchangeService(
        DemoBankContext context,
        IAccountService accountService,
        ICurrencyService currencyService,
        INotificationHelper notificationHelper)
    {
        _context = context;
        _accountService = accountService;
        _currencyService = currencyService;
        _notificationHelper = notificationHelper;
    }

    public async Task<ExchangeResultDto> ExchangeCurrencyAsync(Guid userId, ExchangeRequestDto exchangeDto)
    {
        try
        {
            // Get source account
            var fromAccount = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == exchangeDto.FromAccountId);

            if (fromAccount == null)
                throw new InvalidOperationException("Source account not found");

            if (fromAccount.UserId != userId)
                throw new UnauthorizedAccessException("You don't own this account");

            if (!fromAccount.IsActive)
                throw new InvalidOperationException("Source account is not active");

            // Get or create destination account in target currency
            Account toAccount = null;

            if (exchangeDto.ToAccountId.HasValue)
            {
                toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == exchangeDto.ToAccountId.Value);

                if (toAccount == null)
                    throw new InvalidOperationException("Destination account not found");

                if (toAccount.UserId != userId)
                    throw new UnauthorizedAccessException("You don't own the destination account");

                if (toAccount.Currency != exchangeDto.ToCurrency.ToUpper())
                    throw new InvalidOperationException($"Destination account currency ({toAccount.Currency}) doesn't match requested currency ({exchangeDto.ToCurrency})");
            }
            else
            {
                // Find user's priority account for the target currency
                toAccount = await _accountService.GetPriorityAccountAsync(userId, exchangeDto.ToCurrency);

                if (toAccount == null)
                {
                    // No account in target currency exists
                    throw new InvalidOperationException($"No {exchangeDto.ToCurrency} account found. Please create one first or specify a destination account.");
                }
            }

            if (!toAccount.IsActive)
                throw new InvalidOperationException("Destination account is not active");

            if (fromAccount.Id == toAccount.Id)
                throw new InvalidOperationException("Cannot exchange to the same account");

            if (fromAccount.Currency == toAccount.Currency)
                throw new InvalidOperationException("Accounts have the same currency");

            // Calculate exchange
            var exchangeRate = await _currencyService.GetExchangeRateAsync(
                fromAccount.Currency,
                toAccount.Currency
            );

            var amountInFromCurrency = exchangeDto.Amount;
            var amountBeforeFee = await _currencyService.ConvertCurrencyAsync(
                exchangeDto.Amount,
                fromAccount.Currency,
                toAccount.Currency
            );

            // Calculate fee
            var feeAmount = amountBeforeFee * EXCHANGE_FEE_RATE;
            var feeInUSD = toAccount.Currency == "USD"
                ? feeAmount
                : await _currencyService.ConvertCurrencyAsync(feeAmount, toAccount.Currency, "USD");

            // Apply minimum fee
            if (feeInUSD < MIN_EXCHANGE_FEE)
            {
                feeInUSD = MIN_EXCHANGE_FEE;
                feeAmount = toAccount.Currency == "USD"
                    ? MIN_EXCHANGE_FEE
                    : await _currencyService.ConvertCurrencyAsync(MIN_EXCHANGE_FEE, "USD", toAccount.Currency);
            }

            var amountAfterFee = amountBeforeFee - feeAmount;

            // Check balance
            if (fromAccount.Balance < amountInFromCurrency)
                throw new InvalidOperationException($"Insufficient balance. Available: {fromAccount.Balance:N2}");

            // Update balances
            fromAccount.Balance -= amountInFromCurrency;
            toAccount.Balance += amountAfterFee;
            fromAccount.UpdatedAt = DateTime.UtcNow;
            toAccount.UpdatedAt = DateTime.UtcNow;

            // Create transaction records
            var exchangeTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Type = TransactionType.ExchangeCurrency,
                Amount = exchangeDto.Amount,
                Currency = fromAccount.Currency,
                ExchangeRate = exchangeRate,
                AmountInAccountCurrency = amountInFromCurrency,
                Description = exchangeDto.Description ?? $"Currency exchange {fromAccount.Currency} to {toAccount.Currency}",
                BalanceAfter = fromAccount.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var receiveTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = toAccount.Id,
                Type = TransactionType.Deposit,
                Amount = amountAfterFee,
                Currency = toAccount.Currency,
                AmountInAccountCurrency = amountAfterFee,
                Description = $"Received from currency exchange (Fee: {feeAmount:N2})",
                BalanceAfter = toAccount.Balance,
                Status = TransactionStatus.Completed,
                RelatedTransactionId = exchangeTransaction.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Record fee as separate transaction
            var feeTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = toAccount.Id,
                Type = TransactionType.Fee,
                Amount = feeAmount,
                Currency = toAccount.Currency,
                AmountInAccountCurrency = feeAmount,
                Description = "Exchange fee",
                BalanceAfter = toAccount.Balance,
                Status = TransactionStatus.Completed,
                RelatedTransactionId = exchangeTransaction.Id,
                CreatedAt = DateTime.UtcNow
            };

            exchangeTransaction.RelatedTransactionId = receiveTransaction.Id;

            _context.Transactions.AddRange(exchangeTransaction, receiveTransaction, feeTransaction);

            // Record exchange rate history
            var rateHistory = new ExchangeRateHistory
            {
                Id = Guid.NewGuid(),
                FromCurrency = fromAccount.Currency,
                ToCurrency = toAccount.Currency,
                Rate = exchangeRate,
                RecordedAt = DateTime.UtcNow
            };

            _context.ExchangeRateHistories.Add(rateHistory);

            await _context.SaveChangesAsync();

            // Send notification
            var fromCurrencyInfo = await _currencyService.GetCurrencyAsync(fromAccount.Currency);
            var toCurrencyInfo = await _currencyService.GetCurrencyAsync(toAccount.Currency);

            await _notificationHelper.CreateNotification(
                userId,
                "Currency Exchange Completed",
                $"Exchanged {fromCurrencyInfo.Symbol}{amountInFromCurrency:N2} to {toCurrencyInfo.Symbol}{amountAfterFee:N2} " +
                $"(Rate: {exchangeRate:N4}, Fee: {toCurrencyInfo.Symbol}{feeAmount:N2})",
                NotificationType.Transaction
            );

            // Check rate alerts
            await CheckAndTriggerRateAlertsAsync(fromAccount.Currency, toAccount.Currency, exchangeRate);

            return new ExchangeResultDto
            {
                Success = true,
                TransactionId = exchangeTransaction.Id,
                FromAccount = fromAccount.AccountNumber,
                ToAccount = toAccount.AccountNumber,
                FromAmount = amountInFromCurrency,
                FromCurrency = fromAccount.Currency,
                ToAmount = amountAfterFee,
                ToCurrency = toAccount.Currency,
                ExchangeRate = exchangeRate,
                FeeAmount = feeAmount,
                FeePercentage = EXCHANGE_FEE_RATE * 100,
                NewFromBalance = fromAccount.Balance,
                NewToBalance = toAccount.Balance,
                Timestamp = DateTime.UtcNow
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task<ExchangeQuoteDto> GetExchangeQuoteAsync(string fromCurrency, string toCurrency, decimal amount)
    {
        if (fromCurrency.ToUpper() == toCurrency.ToUpper())
            throw new InvalidOperationException("Currencies must be different");

        var rate = await _currencyService.GetExchangeRateAsync(fromCurrency, toCurrency);
        var convertedAmount = await _currencyService.ConvertCurrencyAsync(amount, fromCurrency, toCurrency);

        // Calculate fee
        var feeAmount = convertedAmount * EXCHANGE_FEE_RATE;
        var feeInUSD = toCurrency.ToUpper() == "USD"
            ? feeAmount
            : await _currencyService.ConvertCurrencyAsync(feeAmount, toCurrency, "USD");

        // Apply minimum fee
        if (feeInUSD < MIN_EXCHANGE_FEE)
        {
            feeInUSD = MIN_EXCHANGE_FEE;
            feeAmount = toCurrency.ToUpper() == "USD"
                ? MIN_EXCHANGE_FEE
                : await _currencyService.ConvertCurrencyAsync(MIN_EXCHANGE_FEE, "USD", toCurrency);
        }

        var amountAfterFee = convertedAmount - feeAmount;

        return new ExchangeQuoteDto
        {
            FromCurrency = fromCurrency.ToUpper(),
            ToCurrency = toCurrency.ToUpper(),
            Amount = amount,
            ExchangeRate = rate,
            ConvertedAmount = convertedAmount,
            FeeAmount = feeAmount,
            FeePercentage = EXCHANGE_FEE_RATE * 100,
            AmountAfterFee = amountAfterFee,
            QuoteValidUntil = DateTime.UtcNow.AddMinutes(5) // Quote valid for 5 minutes
        };
    }

    public async Task<List<ExchangeRateHistoryDto>> GetRateHistoryAsync(string fromCurrency, string toCurrency, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var history = await _context.ExchangeRateHistories
            .Where(h => h.FromCurrency == fromCurrency.ToUpper() &&
                       h.ToCurrency == toCurrency.ToUpper() &&
                       h.RecordedAt >= startDate)
            .OrderBy(h => h.RecordedAt)
            .Select(h => new ExchangeRateHistoryDto
            {
                FromCurrency = h.FromCurrency,
                ToCurrency = h.ToCurrency,
                Rate = h.Rate,
                RecordedAt = h.RecordedAt
            })
            .ToListAsync();

        // If no history, generate some data points based on current rate
        if (!history.Any())
        {
            var currentRate = await _currencyService.GetExchangeRateAsync(fromCurrency, toCurrency);
            var random = new Random();

            for (int i = days; i >= 0; i--)
            {
                // Simulate rate fluctuation (±2%)
                var fluctuation = (decimal)(random.NextDouble() * 0.04 - 0.02);
                var rate = currentRate * (1 + fluctuation);

                history.Add(new ExchangeRateHistoryDto
                {
                    FromCurrency = fromCurrency.ToUpper(),
                    ToCurrency = toCurrency.ToUpper(),
                    Rate = rate,
                    RecordedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
        }

        return history;
    }

    public async Task<List<CurrencyPairDto>> GetFavoritePairsAsync(Guid userId)
    {
        var pairs = await _context.FavoriteCurrencyPairs
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var result = new List<CurrencyPairDto>();

        foreach (var pair in pairs)
        {
            var rate = await _currencyService.GetExchangeRateAsync(pair.FromCurrency, pair.ToCurrency);

            result.Add(new CurrencyPairDto
            {
                Id = pair.Id,
                FromCurrency = pair.FromCurrency,
                ToCurrency = pair.ToCurrency,
                CurrentRate = rate,
                IsFavorite = true
            });
        }

        return result;
    }

    public async Task<bool> AddFavoritePairAsync(Guid userId, string fromCurrency, string toCurrency)
    {
        // Check if pair already exists
        var exists = await _context.FavoriteCurrencyPairs
            .AnyAsync(p => p.UserId == userId &&
                          p.FromCurrency == fromCurrency.ToUpper() &&
                          p.ToCurrency == toCurrency.ToUpper());

        if (exists)
            return false;

        var pair = new FavoriteCurrencyPair
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FromCurrency = fromCurrency.ToUpper(),
            ToCurrency = toCurrency.ToUpper(),
            CreatedAt = DateTime.UtcNow
        };

        _context.FavoriteCurrencyPairs.Add(pair);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveFavoritePairAsync(Guid userId, Guid pairId)
    {
        var pair = await _context.FavoriteCurrencyPairs
            .FirstOrDefaultAsync(p => p.Id == pairId && p.UserId == userId);

        if (pair == null)
            return false;

        _context.FavoriteCurrencyPairs.Remove(pair);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<RateAlertDto>> GetRateAlertsAsync(Guid userId)
    {
        return await _context.ExchangeRateAlerts
            .Where(a => a.UserId == userId)
            .Select(a => new RateAlertDto
            {
                Id = a.Id,
                FromCurrency = a.FromCurrency,
                ToCurrency = a.ToCurrency,
                TargetRate = a.TargetRate,
                AlertType = a.AlertType,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                TriggeredAt = a.TriggeredAt
            })
            .ToListAsync();
    }

    public async Task<RateAlertDto> CreateRateAlertAsync(Guid userId, CreateRateAlertDto alertDto)
    {
        var alert = new ExchangeRateAlert
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FromCurrency = alertDto.FromCurrency.ToUpper(),
            ToCurrency = alertDto.ToCurrency.ToUpper(),
            TargetRate = alertDto.TargetRate,
            AlertType = alertDto.AlertType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExchangeRateAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Check if alert should trigger immediately
        var currentRate = await _currencyService.GetExchangeRateAsync(
            alert.FromCurrency,
            alert.ToCurrency
        );

        if (ShouldTriggerAlert(currentRate, alert.TargetRate, alert.AlertType))
        {
            await TriggerRateAlertAsync(alert, currentRate);
        }

        return new RateAlertDto
        {
            Id = alert.Id,
            FromCurrency = alert.FromCurrency,
            ToCurrency = alert.ToCurrency,
            TargetRate = alert.TargetRate,
            AlertType = alert.AlertType,
            IsActive = alert.IsActive,
            CreatedAt = alert.CreatedAt
        };
    }

    public async Task<bool> DeleteRateAlertAsync(Guid userId, Guid alertId)
    {
        var alert = await _context.ExchangeRateAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId);

        if (alert == null)
            return false;

        _context.ExchangeRateAlerts.Remove(alert);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<ExchangeTransactionDto>> GetExchangeHistoryAsync(Guid userId, int limit = 50)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .Where(t => t.Type == TransactionType.ExchangeCurrency &&
                       t.Account.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new ExchangeTransactionDto
            {
                TransactionId = t.Id,
                FromAccount = t.Account.AccountNumber,
                ToAccount = t.ToAccount.AccountNumber,
                FromAmount = t.Amount,
                FromCurrency = t.Currency,
                ToAmount = t.ToAccount.Balance,
                ToCurrency = t.ToAccount.Currency,
                ExchangeRate = t.ExchangeRate ?? 0,
                Description = t.Description,
                Timestamp = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CurrencyTrendsDto> GetCurrencyTrendsAsync(string currency, int days = 7)
    {
        var trends = new CurrencyTrendsDto
        {
            Currency = currency.ToUpper(),
            Period = $"Last {days} days",
            Trends = new List<CurrencyTrendItem>()
        };

        // Get trends against major currencies
        var majorCurrencies = new[] { "USD", "EUR", "GBP", "JPY" };

        foreach (var targetCurrency in majorCurrencies.Where(c => c != currency.ToUpper()))
        {
            var history = await GetRateHistoryAsync(currency, targetCurrency, days);

            if (history.Any())
            {
                var firstRate = history.First().Rate;
                var lastRate = history.Last().Rate;
                var changePercent = ((lastRate - firstRate) / firstRate) * 100;

                trends.Trends.Add(new CurrencyTrendItem
                {
                    TargetCurrency = targetCurrency,
                    CurrentRate = lastRate,
                    ChangePercent = changePercent,
                    Direction = changePercent > 0 ? "Up" : changePercent < 0 ? "Down" : "Stable",
                    High = history.Max(h => h.Rate),
                    Low = history.Min(h => h.Rate)
                });
            }
        }

        return trends;
    }

    public async Task<List<PopularCurrencyPairDto>> GetPopularPairsAsync()
    {
        // Get most used currency pairs from transactions
        var popularPairs = await _context.Transactions
            .Where(t => t.Type == TransactionType.ExchangeCurrency)
            .GroupBy(t => new { FromCurrency = t.Currency, ToCurrency = t.ToAccount.Currency })
            .Select(g => new
            {
                g.Key.FromCurrency,
                g.Key.ToCurrency,
                Count = g.Count()
            })
            .OrderByDescending(p => p.Count)
            .Take(5)
            .ToListAsync();

        var result = new List<PopularCurrencyPairDto>();

        foreach (var pair in popularPairs)
        {
            var rate = await _currencyService.GetExchangeRateAsync(pair.FromCurrency, pair.ToCurrency);

            result.Add(new PopularCurrencyPairDto
            {
                FromCurrency = pair.FromCurrency,
                ToCurrency = pair.ToCurrency,
                CurrentRate = rate,
                TransactionCount = pair.Count
            });
        }

        // Add default popular pairs if not enough data
        if (result.Count < 5)
        {
            var defaultPairs = new[]
            {
                    ("USD", "EUR"),
                    ("EUR", "GBP"),
                    ("USD", "GBP"),
                    ("USD", "JPY"),
                    ("EUR", "JPY")
                };

            foreach (var (from, to) in defaultPairs)
            {
                if (!result.Any(r => r.FromCurrency == from && r.ToCurrency == to))
                {
                    var rate = await _currencyService.GetExchangeRateAsync(from, to);
                    result.Add(new PopularCurrencyPairDto
                    {
                        FromCurrency = from,
                        ToCurrency = to,
                        CurrentRate = rate,
                        TransactionCount = 0
                    });
                }

                if (result.Count >= 5)
                    break;
            }
        }

        return result.Take(5).ToList();
    }

    public async Task<ExchangeFeeDto> CalculateExchangeFeeAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        var convertedAmount = await _currencyService.ConvertCurrencyAsync(amount, fromCurrency, toCurrency);
        var feeAmount = convertedAmount * EXCHANGE_FEE_RATE;

        var feeInUSD = toCurrency.ToUpper() == "USD"
            ? feeAmount
            : await _currencyService.ConvertCurrencyAsync(feeAmount, toCurrency, "USD");

        // Apply minimum fee
        if (feeInUSD < MIN_EXCHANGE_FEE)
        {
            feeInUSD = MIN_EXCHANGE_FEE;
            feeAmount = toCurrency.ToUpper() == "USD"
                ? MIN_EXCHANGE_FEE
                : await _currencyService.ConvertCurrencyAsync(MIN_EXCHANGE_FEE, "USD", toCurrency);
        }

        return new ExchangeFeeDto
        {
            FeeAmount = feeAmount,
            FeeCurrency = toCurrency.ToUpper(),
            FeePercentage = EXCHANGE_FEE_RATE * 100,
            MinimumFee = MIN_EXCHANGE_FEE,
            IsMinimumApplied = feeInUSD <= MIN_EXCHANGE_FEE
        };
    }

    private bool ShouldTriggerAlert(decimal currentRate, decimal targetRate, string alertType)
    {
        return alertType.ToUpper() switch
        {
            "ABOVE" => currentRate >= targetRate,
            "BELOW" => currentRate <= targetRate,
            _ => false
        };
    }

    private async Task CheckAndTriggerRateAlertsAsync(string fromCurrency, string toCurrency, decimal currentRate)
    {
        var alerts = await _context.ExchangeRateAlerts
            .Include(a => a.User)
            .Where(a => a.IsActive &&
                       a.FromCurrency == fromCurrency &&
                       a.ToCurrency == toCurrency)
            .ToListAsync();

        foreach (var alert in alerts)
        {
            if (ShouldTriggerAlert(currentRate, alert.TargetRate, alert.AlertType))
            {
                await TriggerRateAlertAsync(alert, currentRate);
            }
        }
    }

    private async Task TriggerRateAlertAsync(ExchangeRateAlert alert, decimal currentRate)
    {
        alert.TriggeredAt = DateTime.UtcNow;
        alert.IsActive = false; // Disable after triggering

        await _notificationHelper.CreateNotification(
            alert.UserId,
            "Exchange Rate Alert",
            $"Rate alert triggered: {alert.FromCurrency}/{alert.ToCurrency} is now {currentRate:N4} " +
            $"(Target: {alert.AlertType} {alert.TargetRate:N4})",
            NotificationType.Info
        );

        await _context.SaveChangesAsync();
    }
}