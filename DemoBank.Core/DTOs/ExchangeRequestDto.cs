using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class ExchangeRequestDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    public Guid? ToAccountId { get; set; } // Optional - will use priority account if not specified

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string ToCurrency { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }
}

public class ExchangeResultDto
{
    public bool Success { get; set; }
    public Guid TransactionId { get; set; }
    public string FromAccount { get; set; }
    public string ToAccount { get; set; }
    public decimal FromAmount { get; set; }
    public string FromCurrency { get; set; }
    public decimal ToAmount { get; set; }
    public string ToCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal NewFromBalance { get; set; }
    public decimal NewToBalance { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ExchangeQuoteDto
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal AmountAfterFee { get; set; }
    public DateTime QuoteValidUntil { get; set; }
}

public class ExchangeRateHistoryDto
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class CurrencyPairDto
{
    public Guid Id { get; set; }
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal CurrentRate { get; set; }
    public bool IsFavorite { get; set; }
}

public class AddFavoritePairDto
{
    [Required]
    [MaxLength(3)]
    public string FromCurrency { get; set; }

    [Required]
    [MaxLength(3)]
    public string ToCurrency { get; set; }
}

public class RateAlertDto
{
    public Guid Id { get; set; }
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal TargetRate { get; set; }
    public string AlertType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? TriggeredAt { get; set; }
}

public class CreateRateAlertDto
{
    [Required]
    [MaxLength(3)]
    public string FromCurrency { get; set; }

    [Required]
    [MaxLength(3)]
    public string ToCurrency { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal TargetRate { get; set; }

    [Required]
    [RegularExpression("^(Above|Below)$", ErrorMessage = "AlertType must be 'Above' or 'Below'")]
    public string AlertType { get; set; } // Above or Below
}

public class ExchangeTransactionDto
{
    public Guid TransactionId { get; set; }
    public string FromAccount { get; set; }
    public string ToAccount { get; set; }
    public decimal FromAmount { get; set; }
    public string FromCurrency { get; set; }
    public decimal ToAmount { get; set; }
    public string ToCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CurrencyTrendsDto
{
    public string Currency { get; set; }
    public string Period { get; set; }
    public List<CurrencyTrendItem> Trends { get; set; }
}

public class CurrencyTrendItem
{
    public string TargetCurrency { get; set; }
    public decimal CurrentRate { get; set; }
    public decimal ChangePercent { get; set; }
    public string Direction { get; set; } // Up, Down, Stable
    public decimal High { get; set; }
    public decimal Low { get; set; }
}

public class PopularCurrencyPairDto
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal CurrentRate { get; set; }
    public int TransactionCount { get; set; }
}

public class ExchangeFeeDto
{
    public decimal FeeAmount { get; set; }
    public string FeeCurrency { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal MinimumFee { get; set; }
    public bool IsMinimumApplied { get; set; }
}

public class GetExchangeQuoteDto
{
    [Required]
    [MaxLength(3)]
    public string FromCurrency { get; set; }

    [Required]
    [MaxLength(3)]
    public string ToCurrency { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}