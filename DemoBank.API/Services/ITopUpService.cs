using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface ITopUpService
{
    Task<TopUpResultDto> ProcessTopUpAsync(Guid userId, AccountTopUpDto topUpDto);
    Task<TopUpQuoteDto> GetTopUpQuoteAsync(decimal amount, string currency, PaymentMethod paymentMethod);
    Task<List<PaymentMethodInfoDto>> GetAvailablePaymentMethodsAsync();
    Task<List<TopUpHistoryDto>> GetTopUpHistoryAsync(Guid userId, int limit = 50);
    Task<TopUpLimitsDto> GetTopUpLimitsAsync(Guid userId);
    Task<bool> ValidateTopUpLimitAsync(Guid userId, decimal amount);
    Task<PaymentValidationResultDto> ValidatePaymentMethodAsync(ValidatePaymentMethodDto validationDto);
    Task<TopUpResultDto> ProcessPaymentAsync(AccountTopUpDto topUpDto);
}