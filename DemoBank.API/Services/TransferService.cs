using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class TransferService : ITransferService
{
    private readonly DemoBankContext _context;
    private readonly INotificationHelper _notificationHelper;
    private readonly ICurrencyService _currencyService;
    private readonly ITransactionService _transactionService;

    public TransferService(
        DemoBankContext context,
        INotificationHelper notificationHelper,
        ICurrencyService currencyService,
        ITransactionService transactionService)
    {
        _context = context;
        _notificationHelper = notificationHelper;
        _currencyService = currencyService;
        _transactionService = transactionService;
    }

    public async Task<TransferResultDto> TransferBetweenOwnAccountsAsync(Guid userId, InternalTransferDto transferDto)
    {
        try
        {
            // Get both accounts
            var fromAccount = await _context.Accounts
                .Include(a => a.User)
                .ThenInclude(u => u.Settings)
                .FirstOrDefaultAsync(a => a.Id == transferDto.FromAccountId);

            var toAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == transferDto.ToAccountNumber);

            // Validate accounts
            if (fromAccount == null)
                throw new InvalidOperationException("Source account not found");

            if (toAccount == null)
                throw new InvalidOperationException("Destination account not found");

            if (fromAccount.UserId != userId || toAccount.UserId != userId)
                throw new UnauthorizedAccessException("You can only transfer between your own accounts");

            if (!fromAccount.IsActive)
                throw new InvalidOperationException("Source account is not active");

            if (!toAccount.IsActive)
                throw new InvalidOperationException("Destination account is not active");

            if (fromAccount.Id == toAccount.Id)
                throw new InvalidOperationException("Cannot transfer to the same account");

            // Calculate amounts with currency conversion
            decimal amountInFromCurrency = transferDto.Amount;
            decimal amountInToCurrency = transferDto.Amount;
            decimal? exchangeRate = null;

            if (fromAccount.Currency != toAccount.Currency)
            {
                exchangeRate = await _currencyService.GetExchangeRateAsync(
                    fromAccount.Currency,
                    toAccount.Currency
                );
                amountInToCurrency = await _currencyService.ConvertCurrencyAsync(
                    transferDto.Amount,
                    fromAccount.Currency,
                    toAccount.Currency
                );
            }

            // Check balance
            if (fromAccount.Balance < amountInFromCurrency)
                throw new InvalidOperationException($"Insufficient balance. Available: {fromAccount.Balance:N2}");

            // No daily limit check for internal transfers (optional business rule)
            // You can enable it if needed

            // Update balances
            fromAccount.Balance -= amountInFromCurrency;
            toAccount.Balance += amountInToCurrency;
            fromAccount.UpdatedAt = DateTime.UtcNow;
            toAccount.UpdatedAt = DateTime.UtcNow;

            // Create transaction records
            var fromTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Type = TransactionType.Transfer,
                Amount = transferDto.Amount,
                Currency = fromAccount.Currency,
                AmountInAccountCurrency = amountInFromCurrency,
                Description = transferDto.Description ?? $"Transfer to {toAccount.AccountNumber}",
                BalanceAfter = fromAccount.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var toTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = toAccount.Id,
                Type = TransactionType.Deposit,
                Amount = amountInToCurrency,
                Currency = toAccount.Currency,
                ExchangeRate = exchangeRate,
                AmountInAccountCurrency = amountInToCurrency,
                Description = transferDto.Description ?? $"Transfer from {fromAccount.AccountNumber}",
                BalanceAfter = toAccount.Balance,
                Status = TransactionStatus.Completed,
                RelatedTransactionId = fromTransaction.Id,
                CreatedAt = DateTime.UtcNow
            };

            fromTransaction.RelatedTransactionId = toTransaction.Id;

            _context.Transactions.AddRange(fromTransaction, toTransaction);
            await _context.SaveChangesAsync();

            // Send notifications
            var fromCurrency = await _currencyService.GetCurrencyAsync(fromAccount.Currency);
            var toCurrency = await _currencyService.GetCurrencyAsync(toAccount.Currency);

            await _notificationHelper.CreateNotification(
                userId,
                "Transfer Successful",
                $"Transfer of {fromCurrency.Symbol}{amountInFromCurrency:N2} from {fromAccount.AccountNumber} to {toAccount.AccountNumber} completed. " +
                (exchangeRate.HasValue ? $"Exchange rate: {exchangeRate.Value:N4}" : ""),
                NotificationType.Transaction
            );


            return new TransferResultDto
            {
                Success = true,
                TransactionId = fromTransaction.Id,
                FromAccount = fromAccount.AccountNumber,
                ToAccount = toAccount.AccountNumber,
                Amount = transferDto.Amount,
                Currency = fromAccount.Currency,
                ConvertedAmount = amountInToCurrency,
                ConvertedCurrency = toAccount.Currency,
                ExchangeRate = exchangeRate,
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

    public async Task<TransferResultDto> TransferToAnotherUserAsync(Guid fromUserId, ExternalTransferDto transferDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Get source account
            var fromAccount = await _context.Accounts
                .Include(a => a.User)
                .ThenInclude(u => u.Settings)
                .FirstOrDefaultAsync(a => a.Id == transferDto.FromAccountId);

            if (fromAccount == null)
                throw new InvalidOperationException("Source account not found");

            if (fromAccount.UserId != fromUserId)
                throw new UnauthorizedAccessException("You don't own this account");

            if (!fromAccount.IsActive)
                throw new InvalidOperationException("Source account is not active");

            // Get destination account
            var toAccount = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AccountNumber == transferDto.ToAccountNumber);

            if (toAccount == null)
                throw new InvalidOperationException("Destination account not found");

            if (!toAccount.IsActive)
                throw new InvalidOperationException("Destination account is not active");

            if (fromAccount.Id == toAccount.Id)
                throw new InvalidOperationException("Cannot transfer to the same account");

            // Calculate amounts with currency conversion
            decimal amountInFromCurrency = transferDto.Amount;
            decimal amountInToCurrency = transferDto.Amount;
            decimal? exchangeRate = null;

            if (fromAccount.Currency != toAccount.Currency)
            {
                exchangeRate = await _currencyService.GetExchangeRateAsync(
                    fromAccount.Currency,
                    toAccount.Currency
                );
                amountInToCurrency = await _currencyService.ConvertCurrencyAsync(
                    transferDto.Amount,
                    fromAccount.Currency,
                    toAccount.Currency
                );
            }

            // Check balance
            if (fromAccount.Balance < amountInFromCurrency)
                throw new InvalidOperationException($"Insufficient balance. Available: {fromAccount.Balance:N2}");

            // Check daily transfer limit
            var amountInUSD = fromAccount.Currency == "USD"
                ? amountInFromCurrency
                : await _currencyService.ConvertCurrencyAsync(
                    amountInFromCurrency,
                    fromAccount.Currency,
                    "USD"
                );

            if (!await _transactionService.ValidateTransferLimitAsync(fromUserId, amountInUSD))
            {
                var settings = fromAccount.User.Settings;
                throw new InvalidOperationException(
                    $"Transfer would exceed daily limit of ${settings.DailyTransferLimit:N2}"
                );
            }

            // Process transfer
            fromAccount.Balance -= amountInFromCurrency;
            toAccount.Balance += amountInToCurrency;
            fromAccount.UpdatedAt = DateTime.UtcNow;
            toAccount.UpdatedAt = DateTime.UtcNow;

            // Create transaction records
            var fromTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Type = TransactionType.Transfer,
                Amount = transferDto.Amount,
                Currency = fromAccount.Currency,
                AmountInAccountCurrency = amountInFromCurrency,
                Description = transferDto.Description ?? $"Transfer to {toAccount.User.FirstName} {toAccount.User.LastName}",
                BalanceAfter = fromAccount.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var toTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = toAccount.Id,
                Type = TransactionType.Deposit,
                Amount = amountInToCurrency,
                Currency = toAccount.Currency,
                ExchangeRate = exchangeRate,
                AmountInAccountCurrency = amountInToCurrency,
                Description = transferDto.Description ?? $"Transfer from {fromAccount.User.FirstName} {fromAccount.User.LastName}",
                BalanceAfter = toAccount.Balance,
                Status = TransactionStatus.Completed,
                RelatedTransactionId = fromTransaction.Id,
                CreatedAt = DateTime.UtcNow
            };

            fromTransaction.RelatedTransactionId = toTransaction.Id;

            _context.Transactions.AddRange(fromTransaction, toTransaction);
            await _context.SaveChangesAsync();

            // Send notifications to both users
            var fromCurrency = await _currencyService.GetCurrencyAsync(fromAccount.Currency);
            var toCurrency = await _currencyService.GetCurrencyAsync(toAccount.Currency);

            // Sender notification
            await _notificationHelper.CreateNotification(
                fromUserId,
                "Transfer Sent",
                $"You sent {fromCurrency.Symbol}{amountInFromCurrency:N2} to {toAccount.User.FirstName} {toAccount.User.LastName}. New balance: {fromCurrency.Symbol}{fromAccount.Balance:N2}",
                NotificationType.Transaction
            );

            // Receiver notification
            await _notificationHelper.CreateNotification(
                toAccount.UserId,
                "Transfer Received",
                $"You received {toCurrency.Symbol}{amountInToCurrency:N2} from {fromAccount.User.FirstName} {fromAccount.User.LastName}. New balance: {toCurrency.Symbol}{toAccount.Balance:N2}",
                NotificationType.Transaction
            );

            await transaction.CommitAsync();

            return new TransferResultDto
            {
                Success = true,
                TransactionId = fromTransaction.Id,
                FromAccount = fromAccount.AccountNumber,
                ToAccount = toAccount.AccountNumber,
                RecipientName = $"{toAccount.User.FirstName} {toAccount.User.LastName}",
                Amount = transferDto.Amount,
                Currency = fromAccount.Currency,
                ConvertedAmount = amountInToCurrency,
                ConvertedCurrency = toAccount.Currency,
                ExchangeRate = exchangeRate,
                NewFromBalance = fromAccount.Balance,
                Timestamp = DateTime.UtcNow
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<TransferHistoryDto>> GetTransferHistoryAsync(Guid userId, int limit = 50)
    {
        var transfers = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .ThenInclude(a => a.User)
            .Where(t => t.Type == TransactionType.Transfer &&
                       (t.Account.UserId == userId || t.ToAccount.UserId == userId))
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var history = new List<TransferHistoryDto>();

        foreach (var transfer in transfers)
        {
            var isOutgoing = transfer.Account.UserId == userId;

            history.Add(new TransferHistoryDto
            {
                TransactionId = transfer.Id,
                Direction = isOutgoing ? "Outgoing" : "Incoming",
                FromAccount = transfer.Account.AccountNumber,
                ToAccount = transfer.ToAccount?.AccountNumber,
                Amount = transfer.Amount,
                Currency = transfer.Currency,
                ExchangeRate = transfer.ExchangeRate,
                Description = transfer.Description,
                Status = transfer.Status.ToString(),
                Timestamp = transfer.CreatedAt,
                CounterpartyName = isOutgoing
                    ? $"{transfer.ToAccount?.User?.FirstName} {transfer.ToAccount?.User?.LastName}"
                    : $"{transfer.Account.User.FirstName} {transfer.Account.User.LastName}"
            });
        }

        return history;
    }

    public async Task<List<TransferHistoryDto>> GetAccountTransfersAsync(Guid accountId, int limit = 50)
    {
        var transfers = await _context.Transactions
            .Include(t => t.Account)
            .ThenInclude(a => a.User)
            .Include(t => t.ToAccount)
            .ThenInclude(a => a.User)
            .Where(t => t.Type == TransactionType.Transfer &&
                       (t.AccountId == accountId || t.ToAccountId == accountId))
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var history = new List<TransferHistoryDto>();

        foreach (var transfer in transfers)
        {
            var isOutgoing = transfer.AccountId == accountId;

            history.Add(new TransferHistoryDto
            {
                TransactionId = transfer.Id,
                Direction = isOutgoing ? "Outgoing" : "Incoming",
                FromAccount = transfer.Account.AccountNumber,
                ToAccount = transfer.ToAccount?.AccountNumber,
                Amount = transfer.Amount,
                Currency = transfer.Currency,
                ExchangeRate = transfer.ExchangeRate,
                Description = transfer.Description,
                Status = transfer.Status.ToString(),
                Timestamp = transfer.CreatedAt,
                CounterpartyName = isOutgoing
                    ? $"{transfer.ToAccount?.User?.FirstName} {transfer.ToAccount?.User?.LastName}"
                    : $"{transfer.Account.User.FirstName} {transfer.Account.User.LastName}"
            });
        }

        return history;
    }

    public async Task<TransferValidationResult> ValidateTransferAsync(Guid fromAccountId, string toAccountNumber, decimal amount)
    {
        var result = new TransferValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        // Check source account
        var fromAccount = await _context.Accounts
            .Include(a => a.User)
            .ThenInclude(u => u.Settings)
            .FirstOrDefaultAsync(a => a.Id == fromAccountId);

        if (fromAccount == null)
        {
            result.IsValid = false;
            result.Errors.Add("Source account not found");
            return result;
        }

        if (!fromAccount.IsActive)
        {
            result.IsValid = false;
            result.Errors.Add("Source account is not active");
        }

        // Check destination account
        var toAccount = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AccountNumber == toAccountNumber);

        if (toAccount == null)
        {
            result.IsValid = false;
            result.Errors.Add("Destination account not found");
            return result;
        }

        if (!toAccount.IsActive)
        {
            result.IsValid = false;
            result.Errors.Add("Destination account is not active");
        }

        if (fromAccount.Id == toAccount.Id)
        {
            result.IsValid = false;
            result.Errors.Add("Cannot transfer to the same account");
        }

        // Check balance
        if (fromAccount.Balance < amount)
        {
            result.IsValid = false;
            result.Errors.Add($"Insufficient balance. Available: {fromAccount.Balance:N2}");
        }

        // Check daily limit (only for external transfers)
        if (fromAccount.UserId != toAccount.UserId)
        {
            var amountInUSD = fromAccount.Currency == "USD"
                ? amount
                : await _currencyService.ConvertCurrencyAsync(amount, fromAccount.Currency, "USD");

            var isWithinLimit = await _transactionService.ValidateTransferLimitAsync(
                fromAccount.UserId,
                amountInUSD
            );

            if (!isWithinLimit)
            {
                result.IsValid = false;
                result.Errors.Add($"Transfer would exceed daily limit of ${fromAccount.User.Settings.DailyTransferLimit:N2}");
            }
        }

        // Set additional info
        result.IsInternalTransfer = fromAccount.UserId == toAccount.UserId;
        result.RecipientName = $"{toAccount.User.FirstName} {toAccount.User.LastName}";
        result.RequiresCurrencyConversion = fromAccount.Currency != toAccount.Currency;

        if (result.RequiresCurrencyConversion)
        {
            result.ExchangeRate = await _currencyService.GetExchangeRateAsync(
                fromAccount.Currency,
                toAccount.Currency
            );
            result.ConvertedAmount = await _currencyService.ConvertCurrencyAsync(
                amount,
                fromAccount.Currency,
                toAccount.Currency
            );
        }

        return result;
    }

    public async Task<TransferStatisticsDto> GetTransferStatisticsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Type == TransactionType.Transfer && t.Account.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var transfers = await query.ToListAsync();

        decimal totalSent = 0;
        int sentCount = 0;
        decimal totalReceived = 0;
        int receivedCount = 0;

        // Get received transfers
        var receivedQuery = _context.Transactions
            .Include(t => t.ToAccount)
            .Where(t => t.Type == TransactionType.Deposit &&
                       t.RelatedTransactionId != null &&
                       t.ToAccount.UserId == userId);

        if (startDate.HasValue)
            receivedQuery = receivedQuery.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            receivedQuery = receivedQuery.Where(t => t.CreatedAt <= endDate.Value);

        var receivedTransfers = await receivedQuery.ToListAsync();

        // Calculate sent
        foreach (var transfer in transfers)
        {
            var amountInUSD = transfer.Currency == "USD"
                ? transfer.Amount
                : await _currencyService.ConvertCurrencyAsync(
                    transfer.Amount, transfer.Currency, "USD");

            totalSent += amountInUSD;
            sentCount++;
        }

        // Calculate received
        foreach (var transfer in receivedTransfers)
        {
            var amountInUSD = transfer.Currency == "USD"
                ? transfer.Amount
                : await _currencyService.ConvertCurrencyAsync(
                    transfer.Amount, transfer.Currency, "USD");

            totalReceived += amountInUSD;
            receivedCount++;
        }

        return new TransferStatisticsDto
        {
            TotalSentUSD = totalSent,
            TotalReceivedUSD = totalReceived,
            SentCount = sentCount,
            ReceivedCount = receivedCount,
            NetTransferUSD = totalReceived - totalSent,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    public async Task<bool> CancelPendingTransferAsync(Guid transferId, Guid userId)
    {
        var transfer = await _context.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transferId);

        if (transfer == null)
            return false;

        if (transfer.Account.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to cancel this transfer");

        if (transfer.Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be cancelled");

        transfer.Status = TransactionStatus.Cancelled;

        // If there's a related transaction, cancel it too
        if (transfer.RelatedTransactionId.HasValue)
        {
            var relatedTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transfer.RelatedTransactionId);

            if (relatedTransaction != null)
                relatedTransaction.Status = TransactionStatus.Cancelled;
        }

        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            userId,
            "Transfer Cancelled",
            $"Transfer of {transfer.Amount:N2} has been cancelled",
            NotificationType.Info
        );

        return true;
    }

    public async Task<TransferDetailsDto> GetTransferDetailsAsync(Guid transferId, Guid userId)
    {
        var transfer = await _context.Transactions
            .Include(t => t.Account)
            .ThenInclude(a => a.User)
            .Include(t => t.ToAccount)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == transferId);

        if (transfer == null)
            return null;

        // Check if user has permission to view this transfer
        if (transfer.Account.UserId != userId && transfer.ToAccount?.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to view this transfer");

        return new TransferDetailsDto
        {
            TransactionId = transfer.Id,
            Type = transfer.Type.ToString(),
            FromAccount = new AccountInfoDto
            {
                AccountNumber = transfer.Account.AccountNumber,
                AccountType = transfer.Account.Type.ToString(),
                Currency = transfer.Account.Currency,
                OwnerName = $"{transfer.Account.User.FirstName} {transfer.Account.User.LastName}"
            },
            ToAccount = transfer.ToAccount != null ? new AccountInfoDto
            {
                AccountNumber = transfer.ToAccount.AccountNumber,
                AccountType = transfer.ToAccount.Type.ToString(),
                Currency = transfer.ToAccount.Currency,
                OwnerName = $"{transfer.ToAccount.User.FirstName} {transfer.ToAccount.User.LastName}"
            } : null,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            ExchangeRate = transfer.ExchangeRate,
            AmountInAccountCurrency = transfer.AmountInAccountCurrency,
            Description = transfer.Description,
            Status = transfer.Status.ToString(),
            CreatedAt = transfer.CreatedAt,
            RelatedTransactionId = transfer.RelatedTransactionId
        };
    }
}