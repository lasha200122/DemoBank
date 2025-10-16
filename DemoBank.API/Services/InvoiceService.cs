using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class InvoiceService : IInvoiceService
{
    private readonly DemoBankContext _context;
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly ICurrencyService _currencyService;
    private readonly INotificationHelper _notificationHelper;

    private const decimal LATE_PAYMENT_FEE_PERCENTAGE = 0.02m; // 2% late fee
    private const decimal MIN_LATE_FEE = 5.00m;
    private const int OVERDUE_DAYS = 30;

    public InvoiceService(
        DemoBankContext context,
        IAccountService accountService,
        ITransactionService transactionService,
        ICurrencyService currencyService,
        INotificationHelper notificationHelper)
    {
        _context = context;
        _accountService = accountService;
        _transactionService = transactionService;
        _currencyService = currencyService;
        _notificationHelper = notificationHelper;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(Guid userId, CreateInvoiceDto invoiceDto)
    {
        // Validate amount
        if (invoiceDto.Amount <= 0)
            throw new InvalidOperationException("Invoice amount must be positive");

        // Validate currency
        var currency = await _currencyService.GetCurrencyAsync(invoiceDto.Currency);
        if (currency == null)
            throw new InvalidOperationException($"Currency {invoiceDto.Currency} is not supported");

        // Generate unique invoice number
        var invoiceNumber = AccountNumberGenerator.GenerateInvoiceNumber();

        // Ensure unique
        while (await _context.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber))
        {
            invoiceNumber = AccountNumberGenerator.GenerateInvoiceNumber();
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InvoiceNumber = invoiceNumber,
            Amount = invoiceDto.Amount,
            Currency = invoiceDto.Currency.ToUpper(),
            Description = invoiceDto.Description,
            Status = InvoiceStatus.Draft,
            DueDate = invoiceDto.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        // Add line items if provided
        if (invoiceDto.LineItems != null && invoiceDto.LineItems.Any())
        {
            // Calculate total from line items
            decimal total = 0;
            foreach (var item in invoiceDto.LineItems)
            {
                total += item.Quantity * item.UnitPrice;
            }

            // Apply tax if specified
            if (invoiceDto.TaxRate.HasValue && invoiceDto.TaxRate > 0)
            {
                total += total * (invoiceDto.TaxRate.Value / 100);
            }

            invoice.Amount = total;

            // Store line items as JSON in description for simplicity
            invoice.Description = $"{invoiceDto.Description}\n\nItems:\n" +
                string.Join("\n", invoiceDto.LineItems.Select(i =>
                    $"- {i.Description}: {i.Quantity} x {currency.Symbol}{i.UnitPrice:N2}"));
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Send notification
        await _notificationHelper.CreateNotification(
            userId,
            "Invoice Created",
            $"Invoice #{invoiceNumber} for {currency.Symbol}{invoice.Amount:N2} has been created.",
            NotificationType.Info
        );

        return MapToInvoiceDto(invoice);
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return null;

        return MapToInvoiceDto(invoice);
    }

    public async Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        var invoice = await _context.Invoices
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

        if (invoice == null)
            return null;

        return MapToInvoiceDto(invoice);
    }

    public async Task<List<InvoiceDto>> GetUserInvoicesAsync(Guid userId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        // Check for overdue invoices
        foreach (var invoice in invoices.Where(i =>
            i.Status == InvoiceStatus.Sent &&
            i.DueDate < DateTime.UtcNow))
        {
            invoice.Status = InvoiceStatus.Overdue;
        }

        await _context.SaveChangesAsync();

        return invoices.Select(MapToInvoiceDto).ToList();
    }

    public async Task<List<InvoiceDto>> GetPendingInvoicesAsync(Guid userId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.UserId == userId &&
                       (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue))
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        return invoices.Select(MapToInvoiceDto).ToList();
    }

    public async Task<InvoicePaymentResultDto> PayInvoiceAsync(Guid userId, Guid invoiceId, PayInvoiceDto paymentDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            if (invoice.Status == InvoiceStatus.Paid)
                throw new InvalidOperationException("Invoice has already been paid");

            if (invoice.Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException("Cannot pay a cancelled invoice");

            // Get payment account
            Account paymentAccount;
            if (paymentDto.AccountId.HasValue)
            {
                paymentAccount = await _accountService.GetByIdAsync(paymentDto.AccountId.Value);
                if (paymentAccount.UserId != userId)
                    throw new UnauthorizedAccessException("You don't own this account");
            }
            else
            {
                paymentAccount = await _accountService.GetPriorityAccountAsync(userId, invoice.Currency);
                if (paymentAccount == null)
                {
                    paymentAccount = await _accountService.GetPriorityAccountAsync(userId, "EUR");
                }
            }

            if (paymentAccount == null)
                throw new InvalidOperationException("No valid account for payment");

            // Calculate payment amount
            decimal paymentAmount = invoice.Amount;
            decimal lateFee = 0;

            // Add late fee if overdue
            if (invoice.Status == InvoiceStatus.Overdue)
            {
                lateFee = invoice.Amount * LATE_PAYMENT_FEE_PERCENTAGE;
                if (lateFee < MIN_LATE_FEE)
                    lateFee = MIN_LATE_FEE;

                paymentAmount += lateFee;
            }

            // Convert currency if needed
            decimal amountInAccountCurrency = paymentAmount;
            if (paymentAccount.Currency != invoice.Currency)
            {
                amountInAccountCurrency = await _currencyService.ConvertCurrencyAsync(
                    paymentAmount,
                    invoice.Currency,
                    paymentAccount.Currency
                );
            }

            // Check balance
            if (paymentAccount.Balance < amountInAccountCurrency)
                throw new InvalidOperationException($"Insufficient balance. Required: {amountInAccountCurrency:N2}");

            // Process payment
            paymentAccount.Balance -= amountInAccountCurrency;
            paymentAccount.UpdatedAt = DateTime.UtcNow;

            // Update invoice
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidDate = DateTime.UtcNow;

            // Create transaction
            var paymentTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = paymentAccount.Id,
                Type = TransactionType.Withdrawal,
                Amount = amountInAccountCurrency,
                Currency = paymentAccount.Currency,
                AmountInAccountCurrency = amountInAccountCurrency,
                Description = $"Invoice payment - {invoice.InvoiceNumber}" +
                            (lateFee > 0 ? $" (includes late fee: {lateFee:N2})" : ""),
                BalanceAfter = paymentAccount.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            // Send notifications
            await _notificationHelper.CreateNotification(
                userId,
                "Invoice Paid",
                $"Invoice #{invoice.InvoiceNumber} for {invoice.Currency} {invoice.Amount:N2} has been paid successfully." +
                (lateFee > 0 ? $" Late fee of {lateFee:N2} was applied." : ""),
                NotificationType.Success
            );

            // Notify invoice creator (if different from payer)
            if (invoice.UserId != userId)
            {
                await _notificationHelper.CreateNotification(
                    invoice.UserId,
                    "Invoice Payment Received",
                    $"Payment received for invoice #{invoice.InvoiceNumber}",
                    NotificationType.Success
                );
            }

            await transaction.CommitAsync();

            return new InvoicePaymentResultDto
            {
                Success = true,
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                AmountPaid = paymentAmount,
                LateFee = lateFee,
                Currency = invoice.Currency,
                PaymentAccount = paymentAccount.AccountNumber,
                NewBalance = paymentAccount.Balance,
                Message = "Invoice paid successfully"
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> SendInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);

        if (invoice == null)
            return false;

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be sent");

        invoice.Status = InvoiceStatus.Sent;
        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            invoice.UserId,
            "Invoice Sent",
            $"Invoice #{invoice.InvoiceNumber} has been sent. Due date: {invoice.DueDate:yyyy-MM-dd}",
            NotificationType.Info
        );

        return true;
    }

    public async Task<bool> CancelInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);

        if (invoice == null)
            return false;

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice");

        invoice.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            invoice.UserId,
            "Invoice Cancelled",
            $"Invoice #{invoice.InvoiceNumber} has been cancelled.",
            NotificationType.Warning
        );

        return true;
    }

    public async Task<InvoiceDto> UpdateInvoiceAsync(Guid invoiceId, UpdateInvoiceDto updateDto)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);

        if (invoice == null)
            throw new InvalidOperationException("Invoice not found");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be updated");

        if (updateDto.Amount.HasValue)
            invoice.Amount = updateDto.Amount.Value;

        if (!string.IsNullOrEmpty(updateDto.Description))
            invoice.Description = updateDto.Description;

        if (updateDto.DueDate.HasValue)
            invoice.DueDate = updateDto.DueDate.Value;

        await _context.SaveChangesAsync();

        return MapToInvoiceDto(invoice);
    }

    public async Task<List<InvoiceDto>> SearchInvoicesAsync(Guid userId, InvoiceSearchDto searchDto)
    {
        var query = _context.Invoices
            .Where(i => i.UserId == userId);

        if (!string.IsNullOrEmpty(searchDto.InvoiceNumber))
            query = query.Where(i => i.InvoiceNumber.Contains(searchDto.InvoiceNumber));

        if (searchDto.Status.HasValue)
            query = query.Where(i => i.Status == searchDto.Status.Value);

        if (searchDto.FromDate.HasValue)
            query = query.Where(i => i.CreatedAt >= searchDto.FromDate.Value);

        if (searchDto.ToDate.HasValue)
            query = query.Where(i => i.CreatedAt <= searchDto.ToDate.Value);

        if (searchDto.MinAmount.HasValue)
            query = query.Where(i => i.Amount >= searchDto.MinAmount.Value);

        if (searchDto.MaxAmount.HasValue)
            query = query.Where(i => i.Amount <= searchDto.MaxAmount.Value);

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(MapToInvoiceDto).ToList();
    }

    public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(Guid userId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var summary = new InvoiceSummaryDto
        {
            TotalInvoices = invoices.Count,
            DraftInvoices = invoices.Count(i => i.Status == InvoiceStatus.Draft),
            SentInvoices = invoices.Count(i => i.Status == InvoiceStatus.Sent),
            PaidInvoices = invoices.Count(i => i.Status == InvoiceStatus.Paid),
            OverdueInvoices = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
            CancelledInvoices = invoices.Count(i => i.Status == InvoiceStatus.Cancelled),
            TotalAmount = invoices.Where(i => i.Status != InvoiceStatus.Cancelled).Sum(i => i.Amount),
            TotalPaid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
            TotalPending = invoices.Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue).Sum(i => i.Amount)
        };

        return summary;
    }

    public async Task<bool> MarkAsOverdueAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);

        if (invoice == null)
            return false;

        if (invoice.Status != InvoiceStatus.Sent)
            return false;

        if (invoice.DueDate >= DateTime.UtcNow)
            return false;

        invoice.Status = InvoiceStatus.Overdue;
        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            invoice.UserId,
            "Invoice Overdue",
            $"Invoice #{invoice.InvoiceNumber} is now overdue. Late fees may apply.",
            NotificationType.Warning
        );

        return true;
    }

    public async Task<List<InvoiceDto>> GetOverdueInvoicesAsync(Guid userId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.UserId == userId && i.Status == InvoiceStatus.Overdue)
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        return invoices.Select(MapToInvoiceDto).ToList();
    }

    public async Task ProcessOverdueInvoicesAsync()
    {
        var overdueInvoices = await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var invoice in overdueInvoices)
        {
            await MarkAsOverdueAsync(invoice.Id);
        }
    }

    private InvoiceDto MapToInvoiceDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = invoice.Amount,
            Currency = invoice.Currency,
            Description = invoice.Description,
            Status = invoice.Status.ToString(),
            DueDate = invoice.DueDate,
            PaidDate = invoice.PaidDate,
            CreatedAt = invoice.CreatedAt,
            IsOverdue = invoice.Status == InvoiceStatus.Sent && invoice.DueDate < DateTime.UtcNow
        };
    }
}