using DemoBank.Core.DTOs;
using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface IAccountService
{
    Task<Account> GetByIdAsync(Guid accountId);
    Task<List<Account>> GetAccountByUserIdAsync(Guid userId);
    Task<Account> GetByAccountNumberAsync(string accountNumber);
    Task<List<Account>> GetUserAccountsAsync(Guid userId);
    Task<List<Account>> GetActiveUserAccountsAsync(Guid userId);
    Task<Account> CreateAccountAsync(Guid userId, CreateAccountDto createDto);
    Task<bool> UpdateAccountAsync(Account account);
    Task<bool> ActivateAccountAsync(Guid accountId);
    Task<bool> DeactivateAccountAsync(Guid accountId);
    Task<bool> SetPriorityAccountAsync(Guid userId, Guid accountId, string currency);
    Task<Account> GetPriorityAccountAsync(Guid userId, string currency);
    Task<decimal> GetTotalBalanceInUSDAsync(Guid userId);
    Task<Dictionary<string, decimal>> GetBalancesByCurrencyAsync(Guid userId);
    Task<bool> UserOwnsAccountAsync(Guid userId, Guid accountId);
    Task<List<Transaction>> GetAccountTransactionsAsync(Guid accountId, int limit = 10);
}