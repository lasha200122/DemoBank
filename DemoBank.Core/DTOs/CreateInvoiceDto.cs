using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoBank.Core.Models;

namespace DemoBank.Core.DTOs;

public class CreateInvoiceDto
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public List<InvoiceLineItemDto> LineItems { get; set; }

    public decimal? TaxRate { get; set; }

    public string RecipientEmail { get; set; }

    public string RecipientName { get; set; }
}

public class InvoiceLineItemDto
{
    [Required]
    public string Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class UpdateInvoiceDto
{
    public decimal? Amount { get; set; }
    public string Description { get; set; }
    public DateTime? DueDate { get; set; }
}

public class PayInvoiceDto
{
    public Guid? AccountId { get; set; } // Optional, will use priority account if not specified
}

public class InvoicePaymentResultDto
{
    public bool Success { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal LateFee { get; set; }
    public string Currency { get; set; }
    public string PaymentAccount { get; set; }
    public decimal NewBalance { get; set; }
    public string Message { get; set; }
}

public class InvoiceSearchDto
{
    public string InvoiceNumber { get; set; }
    public InvoiceStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public class InvoiceSummaryDto
{
    public int TotalInvoices { get; set; }
    public int DraftInvoices { get; set; }
    public int SentInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public int CancelledInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
}