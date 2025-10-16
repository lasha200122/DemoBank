using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class InternalTransferDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    [Required]
    public string ToAccountNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }
}

public class ExternalTransferDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    [Required]
    public string ToAccountNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public string TransferReference { get; set; } // Optional reference number
}

public class QuickTransferDto
{
    [Required]
    public string FromAccountNumber { get; set; }

    [Required]
    public string ToAccountNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } // Optional, defaults to source account currency

    [MaxLength(500)]
    public string Description { get; set; }
}

public class TransferResultDto
{
    public bool Success { get; set; }
    public Guid TransactionId { get; set; }
    public string FromAccount { get; set; }
    public string ToAccount { get; set; }
    public string RecipientName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string ConvertedCurrency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal NewFromBalance { get; set; }
    public decimal? NewToBalance { get; set; }
    public DateTime Timestamp { get; set; }
    public string TransferReference { get; set; }
}

public class TransferHistoryDto
{
    public Guid TransactionId { get; set; }
    public string Direction { get; set; } // Incoming/Outgoing
    public string FromAccount { get; set; }
    public string ToAccount { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string CounterpartyName { get; set; }
}

public class TransferValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public bool IsInternalTransfer { get; set; }
    public string RecipientName { get; set; }
    public bool RequiresCurrencyConversion { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? ConvertedAmount { get; set; }
}

public class TransferStatisticsDto
{
    public decimal TotalSentEUR { get; set; }
    public decimal TotalReceivedEUR { get; set; }
    public int SentCount { get; set; }
    public int ReceivedCount { get; set; }
    public decimal NetTransferEUR { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class TransferDetailsDto
{
    public Guid TransactionId { get; set; }
    public string Type { get; set; }
    public AccountInfoDto FromAccount { get; set; }
    public AccountInfoDto ToAccount { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal AmountInAccountCurrency { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? RelatedTransactionId { get; set; }
}

public class AccountInfoDto
{
    public string AccountNumber { get; set; }
    public string AccountType { get; set; }
    public string Currency { get; set; }
    public string OwnerName { get; set; }
}

public class ValidateTransferDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    [Required]
    public string ToAccountNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

public class BulkTransferDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    [Required]
    public List<BulkTransferItem> Transfers { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }
}

public class BulkTransferItem
{
    [Required]
    public string ToAccountNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string Reference { get; set; }
}

public class ScheduledTransferDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    [Required]
    public string ToAccountNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime ScheduledDate { get; set; }

    public string Frequency { get; set; } // Once, Weekly, Monthly

    [MaxLength(500)]
    public string Description { get; set; }
}