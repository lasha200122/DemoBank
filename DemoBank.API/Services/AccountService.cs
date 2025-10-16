using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class AccountService : IAccountService
{
    private readonly DemoBankContext _context;
    private readonly INotificationHelper _notificationHelper;
    private readonly ICurrencyService _currencyService;

    public AccountService(
        DemoBankContext context,
        INotificationHelper notificationHelper,
        ICurrencyService currencyService)
    {
        _context = context;
        _notificationHelper = notificationHelper;
        _currencyService = currencyService;
    }
    public async Task<List<Account>> GetAccountByUserIdAsync(Guid userId)
    {
        return await _context.Accounts
        .Include(a => a.User)
        .Where(a => a.UserId == userId) // აი აქ UserId-ზე ფილტრი
        .ToListAsync();
    }
    public async Task<Account> GetByIdAsync(Guid accountId)
    {
        return await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<Account> GetByAccountNumberAsync(string accountNumber)
    {
        return await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<List<Account>> GetUserAccountsAsync(Guid userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Account>> GetActiveUserAccountsAsync(Guid userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Account> CreateAccountAsync(Guid userId, CreateAccountDto createDto)
    {
        // Verify user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Verify currency is supported
        var currency = await _currencyService.GetCurrencyAsync(createDto.Currency);
        if (currency == null)
            throw new InvalidOperationException($"Currency {createDto.Currency} is not supported");

        // Parse account type
        if (!Enum.TryParse<AccountType>(createDto.Type, out var accountType))
            throw new InvalidOperationException("Invalid account type");

        // Generate unique account number
        string accountNumber;
        do
        {
            accountNumber = AccountNumberGenerator.GenerateAccountNumber();
        } while (await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

        // Create new account
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            UserId = userId,
            Type = accountType,
            Currency = createDto.Currency.ToUpper(),
            Balance = 0,
            IsPriority = createDto.IsPriority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Title = createDto.Title
        };

        // If this is set as priority, remove priority from other accounts with same currency
        if (createDto.IsPriority)
        {
            var existingPriorityAccounts = await _context.Accounts
                .Where(a => a.UserId == userId &&
                       a.Currency == account.Currency &&
                       a.IsPriority)
                .ToListAsync();

            foreach (var acc in existingPriorityAccounts)
            {
                acc.IsPriority = false;
                acc.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.Accounts.Add(account);

        // Handle initial deposit if provided
        //if (createDto.InitialDeposit > 0)
        //{
        //    var transaction = new Transaction
        //    {
        //        Id = Guid.NewGuid(),
        //        AccountId = account.Id,
        //        Type = TransactionType.Deposit,
        //        Amount = createDto.InitialDeposit,
        //        Currency = account.Currency,
        //        AmountInAccountCurrency = createDto.InitialDeposit,
        //        Description = "Initial deposit",
        //        BalanceAfter = createDto.InitialDeposit,
        //        Status = TransactionStatus.Completed,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    account.Balance = createDto.InitialDeposit;
        //    _context.Transactions.Add(transaction);
        //}

        // Send notification
        await _notificationHelper.CreateNotification(
            userId,
            "New Account Created",
            $"Your new {accountType} account ({accountNumber}) has been created successfully.",
            NotificationType.Success
        );

        await _context.SaveChangesAsync();

        return account;
    }

    public async Task<bool> UpdateAccountAsync(Account account)
    {
        account.UpdatedAt = DateTime.UtcNow;
        _context.Accounts.Update(account);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ActivateAccountAsync(Guid accountId)
    {
        var account = await GetByIdAsync(accountId);
        if (account == null)
            return false;

        account.IsActive = true;
        account.UpdatedAt = DateTime.UtcNow;

        await _notificationHelper.CreateNotification(
            account.UserId,
            "Account Activated",
            $"Your account {account.AccountNumber} has been activated.",
            NotificationType.Info
        );

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeactivateAccountAsync(Guid accountId)
    {
        var account = await GetByIdAsync(accountId);
        if (account == null)
            return false;

        // Check if account has balance
        if (account.Balance > 0)
            throw new InvalidOperationException("Cannot deactivate account with positive balance");

        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;

        await _notificationHelper.CreateNotification(
            account.UserId,
            "Account Deactivated",
            $"Your account {account.AccountNumber} has been deactivated.",
            NotificationType.Warning
        );

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SetPriorityAccountAsync(Guid userId, Guid accountId, string currency)
    {
        var account = await GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
            return false;

        if (account.Currency != currency.ToUpper())
            throw new InvalidOperationException("Account currency does not match");

        // Remove priority from other accounts with same currency
        var existingPriorityAccounts = await _context.Accounts
            .Where(a => a.UserId == userId &&
                   a.Currency == currency.ToUpper() &&
                   a.IsPriority)
            .ToListAsync();

        foreach (var acc in existingPriorityAccounts)
        {
            acc.IsPriority = false;
            acc.UpdatedAt = DateTime.UtcNow;
        }

        // Set this account as priority
        account.IsPriority = true;
        account.UpdatedAt = DateTime.UtcNow;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<Account> GetPriorityAccountAsync(Guid userId, string currency)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId &&
                                     a.Currency == currency.ToUpper() &&
                                     a.IsPriority &&
                                     a.IsActive);
    }

    public async Task<decimal> GetTotalBalanceInUSDAsync(Guid userId)
    {
        var accounts = await GetActiveUserAccountsAsync(userId);
        decimal totalInUSD = 0;

        foreach (var account in accounts)
        {
            if (account.Currency == "USD")
            {
                totalInUSD += account.Balance;
            }
            else
            {
                var rate = await _currencyService.GetExchangeRateAsync(account.Currency, "USD");
                totalInUSD += account.Balance * rate;
            }
        }

        return totalInUSD;
    }

    public async Task<decimal> GetTotalBalanceInEURAsync(Guid userId)
    {
        var accounts = await GetActiveUserAccountsAsync(userId);
        decimal totalInEUR = 0;

        foreach (var account in accounts)
        {
            if (account.Currency == "EUR")
            {
                totalInEUR += account.Balance;
            }
            else
            {
                var rate = await _currencyService.GetExchangeRateAsync(account.Currency, "EUR");
                totalInEUR += account.Balance * rate;
            }
        }

        return totalInEUR;
    }

    public async Task<Dictionary<string, decimal>> GetBalancesByCurrencyAsync(Guid userId)
    {
        var accounts = await GetActiveUserAccountsAsync(userId);
        var balances = new Dictionary<string, decimal>();

        foreach (var account in accounts)
        {
            if (balances.ContainsKey(account.Currency))
                balances[account.Currency] += account.Balance;
            else
                balances[account.Currency] = account.Balance;
        }

        return balances;
    }

    public async Task<bool> UserOwnsAccountAsync(Guid userId, Guid accountId)
    {
        return await _context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId);
    }

    public async Task<List<Transaction>> GetAccountTransactionsAsync(Guid accountId, int limit = 10)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
