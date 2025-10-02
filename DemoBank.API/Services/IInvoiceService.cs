using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateInvoiceAsync(Guid userId, CreateInvoiceDto invoiceDto);
    Task<InvoiceDto> GetInvoiceByIdAsync(Guid invoiceId);
    Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber);
    Task<List<InvoiceDto>> GetUserInvoicesAsync(Guid userId);
    Task<List<InvoiceDto>> GetPendingInvoicesAsync(Guid userId);
    Task<InvoicePaymentResultDto> PayInvoiceAsync(Guid userId, Guid invoiceId, PayInvoiceDto paymentDto);
    Task<bool> SendInvoiceAsync(Guid invoiceId);
    Task<bool> CancelInvoiceAsync(Guid invoiceId);
    Task<InvoiceDto> UpdateInvoiceAsync(Guid invoiceId, UpdateInvoiceDto updateDto);
    Task<List<InvoiceDto>> SearchInvoicesAsync(Guid userId, InvoiceSearchDto searchDto);
    Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(Guid userId);
    Task<bool> MarkAsOverdueAsync(Guid invoiceId);
    Task<List<InvoiceDto>> GetOverdueInvoicesAsync(Guid userId);
    Task ProcessOverdueInvoicesAsync();
}