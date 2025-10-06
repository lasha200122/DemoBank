using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class AccountTopUpDto
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    // Payment method specific fields
    public CardPaymentDetails? CardDetails { get; set; }
    public BankAccountDetails? BankDetails { get; set; }
    public PayPalDetails? PayPalDetails { get; set; }
}

public class CardPaymentDetails
{
    [CreditCard]
    public string CardNumber { get; set; }

    [MaxLength(100)]
    public string CardHolderName { get; set; }

    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$")]
    public string ExpiryDate { get; set; } // MM/YY format

    [RegularExpression(@"^\d{3,4}$")]
    public string CVV { get; set; }

    [MaxLength(10)]
    public string PostalCode { get; set; }
}

public class BankAccountDetails
{
    [Required]
    public string AccountNumber { get; set; }

    [Required]
    public string RoutingNumber { get; set; }

    [Required]
    [MaxLength(100)]
    public string AccountHolderName { get; set; }

    [Required]
    public BankAccountType AccountType { get; set; }
}

public class PayPalDetails
{
    [EmailAddress]
    public string Email { get; set; }

    public string PayPalTransactionId { get; set; }
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    BankTransfer,
    PayPal,
    ApplePay,
    GooglePay
}

public enum BankAccountType
{
    Checking,
    Savings
}

public class TopUpResultDto
{
    public bool Success { get; set; }
    public Guid TransactionId { get; set; }
    public string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal TotalCharged { get; set; }
    public decimal NewBalance { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string ReferenceNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }
}

public class TopUpQuoteDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal EstimatedArrivalTime { get; set; } // in minutes
    public string FeeExplanation { get; set; }
}

public class PaymentMethodInfoDto
{
    public PaymentMethod Method { get; set; }
    public string DisplayName { get; set; }
    public string Icon { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal MinimumFee { get; set; }
    public decimal MaximumFee { get; set; }
    public bool IsActive { get; set; }
    public int EstimatedProcessingTime { get; set; } // in minutes
}

public class TopUpHistoryDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal ProcessingFee { get; set; }
    public string Status { get; set; }
    public string ReferenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; }
}

public class TopUpLimitsDto
{
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal UsedToday { get; set; }
    public decimal UsedThisMonth { get; set; }
    public decimal RemainingToday { get; set; }
    public decimal RemainingThisMonth { get; set; }
}

public class ValidatePaymentMethodDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public CardPaymentDetails CardDetails { get; set; }
    public BankAccountDetails BankDetails { get; set; }
    public PayPalDetails PayPalDetails { get; set; }
}

public class PaymentValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public string PaymentMethodStatus { get; set; }
}

public class TopUpQuoteRequestDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}

public class ValidateAmountDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

public class AccountSelectionDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; }
    public string AccountType { get; set; }
    public string Currency { get; set; }
    public decimal Balance { get; set; }
    public bool IsPriority { get; set; }
    public string DisplayName { get; set; }
}

public class SimulateTopUpDto
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

public class TopUpStatisticsDto
{
    public string Period { get; set; }
    public int TotalTopUps { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal AverageAmount { get; set; }
    public string MostUsedMethod { get; set; }
    public Dictionary<string, int> TopUpsByMethod { get; set; }
    public Dictionary<string, decimal> TopUpsByCurrency { get; set; }
}