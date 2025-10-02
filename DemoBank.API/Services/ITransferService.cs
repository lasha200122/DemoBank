using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface ITransferService
{
    Task<TransferResultDto> TransferBetweenOwnAccountsAsync(Guid userId, InternalTransferDto transferDto);
    Task<TransferResultDto> TransferToAnotherUserAsync(Guid fromUserId, ExternalTransferDto transferDto);
    Task<List<TransferHistoryDto>> GetTransferHistoryAsync(Guid userId, int limit = 50);
    Task<List<TransferHistoryDto>> GetAccountTransfersAsync(Guid accountId, int limit = 50);
    Task<TransferValidationResult> ValidateTransferAsync(Guid fromAccountId, string toAccountNumber, decimal amount);
    Task<TransferStatisticsDto> GetTransferStatisticsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> CancelPendingTransferAsync(Guid transferId, Guid userId);
    Task<TransferDetailsDto> GetTransferDetailsAsync(Guid transferId, Guid userId);
}