using DemoBank.Core.DTOs;
using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface ITransactionService
{
    Task<Transaction> DepositAsync(Guid accountId, DepositDto depositDto);
    Task<Transaction> WithdrawAsync(Guid accountId, WithdrawalDto withdrawalDto);
    Task<List<Transaction>> GetUserTransactionsAsync(Guid userId, int limit = 50);
    Task<List<Transaction>> GetAccountTransactionsAsync(Guid accountId, int limit = 50);
    Task<List<Transaction>> GetTransactionsByDateRangeAsync(Guid accountId, DateTime startDate, DateTime endDate);
    Task<decimal> GetDailyWithdrawalTotalAsync(Guid userId, DateTime date);
    Task<decimal> GetDailyTransferTotalAsync(Guid userId, DateTime date);
    Task<bool> ValidateWithdrawalLimitAsync(Guid userId, decimal amount);
    Task<bool> ValidateTransferLimitAsync(Guid userId, decimal amount);
    Task<TransactionSummaryDto> GetTransactionSummaryAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<Dictionary<string, decimal>> GetMonthlyStatisticsAsync(Guid userId, int year, int month);
}