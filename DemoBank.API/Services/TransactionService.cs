using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class TransactionService : ITransactionService
{
    private readonly DemoBankContext _context;
    private readonly INotificationHelper _notificationHelper;
    private readonly ICurrencyService _currencyService;

    public TransactionService(
        DemoBankContext context,
        INotificationHelper notificationHelper,
        ICurrencyService currencyService)
    {
        _context = context;
        _notificationHelper = notificationHelper;
        _currencyService = currencyService;
    }

    public async Task<Transaction> DepositAsync(Guid accountId, DepositDto depositDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Get account with lock for update
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                throw new InvalidOperationException("Account not found");

            if (!account.IsActive)
                throw new InvalidOperationException("Account is not active");

            // Calculate amount in account currency if different
            decimal amountInAccountCurrency = depositDto.Amount;
            decimal? exchangeRate = null;
            string transactionCurrency = account.Currency;

            if (!string.IsNullOrEmpty(depositDto.Currency) &&
                depositDto.Currency.ToUpper() != account.Currency)
            {
                exchangeRate = await _currencyService.GetExchangeRateAsync(
                    depositDto.Currency,
                    account.Currency
                );
                amountInAccountCurrency = await _currencyService.ConvertCurrencyAsync(
                    depositDto.Amount,
                    depositDto.Currency,
                    account.Currency
                );
                transactionCurrency = depositDto.Currency.ToUpper();
            }

            // Update account balance
            account.Balance += amountInAccountCurrency;
            account.UpdatedAt = DateTime.UtcNow;

            // Create transaction record
            var transactionRecord = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Type = TransactionType.Deposit,
                Amount = depositDto.Amount,
                Currency = transactionCurrency,
                ExchangeRate = exchangeRate,
                AmountInAccountCurrency = amountInAccountCurrency,
                Description = depositDto.Description ?? "Deposit",
                BalanceAfter = account.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transactionRecord);
            await _context.SaveChangesAsync();

            // Send notification
            var currency = await _currencyService.GetCurrencyAsync(account.Currency);
            await _notificationHelper.CreateNotification(
                account.UserId,
                "Deposit Successful",
                $"Deposit of {currency.Symbol}{amountInAccountCurrency:N2} to account {account.AccountNumber} completed successfully. New balance: {currency.Symbol}{account.Balance:N2}",
                NotificationType.Transaction
            );

            await transaction.CommitAsync();

            return transactionRecord;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Transaction> WithdrawAsync(Guid accountId, WithdrawalDto withdrawalDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Get account with lock for update
            var account = await _context.Accounts
                .Include(a => a.User)
                .ThenInclude(u => u.Settings)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                throw new InvalidOperationException("Account not found");

            if (!account.IsActive)
                throw new InvalidOperationException("Account is not active");

            // Calculate amount in account currency if different
            decimal amountInAccountCurrency = withdrawalDto.Amount;
            decimal? exchangeRate = null;
            string transactionCurrency = account.Currency;

            if (!string.IsNullOrEmpty(withdrawalDto.Currency) &&
                withdrawalDto.Currency.ToUpper() != account.Currency)
            {
                exchangeRate = await _currencyService.GetExchangeRateAsync(
                    withdrawalDto.Currency,
                    account.Currency
                );
                amountInAccountCurrency = await _currencyService.ConvertCurrencyAsync(
                    withdrawalDto.Amount,
                    withdrawalDto.Currency,
                    account.Currency
                );
                transactionCurrency = withdrawalDto.Currency.ToUpper();
            }

            // Check sufficient balance
            if (account.Balance < amountInAccountCurrency)
                throw new InvalidOperationException("Insufficient balance");

            // Check daily withdrawal limit
            var amountInEUR = account.Currency == "EUR"
                ? amountInAccountCurrency
                : await _currencyService.ConvertCurrencyAsync(
                    amountInAccountCurrency,
                    account.Currency,
                    "EUR"
                );

            if (!await ValidateWithdrawalLimitAsync(account.UserId, amountInEUR))
            {
                var settings = account.User.Settings;
                throw new InvalidOperationException(
                    $"Withdrawal would exceed daily limit of €{settings.DailyWithdrawalLimit:N2}"
                );
            }

            // Update account balance
            account.Balance -= amountInAccountCurrency;
            account.UpdatedAt = DateTime.UtcNow;

            // Create transaction record
            var transactionRecord = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Type = TransactionType.Withdrawal,
                Amount = withdrawalDto.Amount,
                Currency = transactionCurrency,
                ExchangeRate = exchangeRate,
                AmountInAccountCurrency = amountInAccountCurrency,
                Description = withdrawalDto.Description ?? "Withdrawal",
                BalanceAfter = account.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transactionRecord);
            await _context.SaveChangesAsync();

            // Send notification
            var currency = await _currencyService.GetCurrencyAsync(account.Currency);
            await _notificationHelper.CreateNotification(
                account.UserId,
                "Withdrawal Successful",
                $"Withdrawal of {currency.Symbol}{amountInAccountCurrency:N2} from account {account.AccountNumber} completed successfully. New balance: {currency.Symbol}{account.Balance:N2}",
                NotificationType.Transaction
            );

            // Send warning if balance is low
            if (account.Balance < 100 && account.Currency == "EUR" ||
                (account.Currency != "EUR" && await _currencyService.ConvertCurrencyAsync(
                    account.Balance, account.Currency, "EUR") < 100))
            {
                await _notificationHelper.CreateNotification(
                    account.UserId,
                    "Low Balance Alert",
                    $"Your account {account.AccountNumber} has a low balance of {currency.Symbol}{account.Balance:N2}",
                    NotificationType.Warning
                );
            }

            await transaction.CommitAsync();

            return transactionRecord;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Transaction>> GetUserTransactionsAsync(Guid userId, int limit = 50)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetAccountTransactionsAsync(Guid accountId, int limit = 50)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsByDateRangeAsync(
        Guid accountId,
        DateTime startDate,
        DateTime endDate)
    {
        return await _context.Transactions
            .Where(t => (t.AccountId == accountId || t.ToAccountId == accountId) &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetDailyWithdrawalTotalAsync(Guid userId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1);

        var transactions = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       t.Type == TransactionType.Withdrawal &&
                       t.Status == TransactionStatus.Completed &&
                       t.CreatedAt >= startOfDay &&
                       t.CreatedAt < endOfDay)
            .ToListAsync();

        decimal totalInEUR = 0;

        foreach (var trans in transactions)
        {
            if (trans.Currency == "EUR")
            {
                totalInEUR += trans.Amount;
            }
            else
            {
                var amountInEUR = await _currencyService.ConvertCurrencyAsync(
                    trans.Amount,
                    trans.Currency,
                    "EUR"
                );
                totalInEUR += amountInEUR;
            }
        }

        return totalInEUR;
    }

    public async Task<decimal> GetDailyTransferTotalAsync(Guid userId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1);

        var transactions = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       t.Type == TransactionType.Transfer &&
                       t.Status == TransactionStatus.Completed &&
                       t.CreatedAt >= startOfDay &&
                       t.CreatedAt < endOfDay)
            .ToListAsync();

        decimal totalInEUR = 0;

        foreach (var trans in transactions)
        {
            if (trans.Currency == "EUR")
            {
                totalInEUR += trans.Amount;
            }
            else
            {
                var amountInEUR = await _currencyService.ConvertCurrencyAsync(
                    trans.Amount,
                    trans.Currency,
                    "EUR"
                );
                totalInEUR += amountInEUR;
            }
        }

        return totalInEUR;
    }

    public async Task<bool> ValidateWithdrawalLimitAsync(Guid userId, decimal amountInEUR)
    {
        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null)
            return true; // No settings means no limits

        var dailyTotal = await GetDailyWithdrawalTotalAsync(userId, DateTime.UtcNow);

        return (dailyTotal + amountInEUR) <= userSettings.DailyWithdrawalLimit;
    }

    public async Task<bool> ValidateTransferLimitAsync(Guid userId, decimal amountInEUR)
    {
        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null)
            return true; // No settings means no limits

        var dailyTotal = await GetDailyTransferTotalAsync(userId, DateTime.UtcNow);

        return (dailyTotal + amountInEUR) <= userSettings.DailyTransferLimit;
    }

    public async Task<TransactionSummaryDto> GetTransactionSummaryAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var transactions = await query.ToListAsync();

        decimal totalDeposits = 0;
        decimal totalWithdrawals = 0;
        decimal totalTransfers = 0;
        int depositCount = 0;
        int withdrawalCount = 0;
        int transferCount = 0;

        foreach (var trans in transactions)
        {
            decimal amountInEUR = trans.Currency == "EUR"
                ? trans.Amount
                : await _currencyService.ConvertCurrencyAsync(
                    trans.Amount, trans.Currency, "EUR");

            switch (trans.Type)
            {
                case TransactionType.Deposit:
                    totalDeposits += amountInEUR;
                    depositCount++;
                    break;
                case TransactionType.Withdrawal:
                    totalWithdrawals += amountInEUR;
                    withdrawalCount++;
                    break;
                case TransactionType.Transfer:
                    totalTransfers += amountInEUR;
                    transferCount++;
                    break;
            }
        }

        return new TransactionSummaryDto
        {
            TotalDepositsEUR = totalDeposits,
            TotalWithdrawalsEUR = totalWithdrawals,
            TotalTransfersEUR = totalTransfers,
            DepositCount = depositCount,
            WithdrawalCount = withdrawalCount,
            TransferCount = transferCount,
            TotalTransactions = transactions.Count,
            StartDate = startDate ?? transactions.MinBy(t => t.CreatedAt)?.CreatedAt,
            EndDate = endDate ?? transactions.MaxBy(t => t.CreatedAt)?.CreatedAt
        };
    }

    public async Task<Dictionary<string, decimal>> GetMonthlyStatisticsAsync(
        Guid userId,
        int year,
        int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var summary = await GetTransactionSummaryAsync(userId, startDate, endDate);

        return new Dictionary<string, decimal>
        {
            ["TotalDeposits"] = summary.TotalDepositsEUR,
            ["TotalWithdrawals"] = summary.TotalWithdrawalsEUR,
            ["TotalTransfers"] = summary.TotalTransfersEUR,
            ["NetCashFlow"] = summary.TotalDepositsEUR - summary.TotalWithdrawalsEUR - summary.TotalTransfersEUR
        };
    }
}