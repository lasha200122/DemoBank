using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class ValidateCryptoAddressDto
{
    public string Address { get; set; }
    public string Currency { get; set; }
    public string Network { get; set; }
}

public class CreateCurrencyDto
{
    [Required]
    [MaxLength(10)]
    public string Code { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; }

    [Required]
    public string Type { get; set; } // "Fiat" or "Crypto"

    [Required]
    [Range(0.0000001, double.MaxValue)]
    public decimal ExchangeRateToUSD { get; set; }

    public string ImageUrl { get; set; }
    public string LogoUrl { get; set; }

    [Required]
    [Range(0, 18)]
    public int DecimalPlaces { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinimumTransactionAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MaximumTransactionAmount { get; set; }

    public string Network { get; set; }
    public string ContractAddress { get; set; }

    [Range(0, double.MaxValue)]
    public decimal NetworkFee { get; set; }

    [Range(0, 100)]
    public int ConfirmationsRequired { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateCurrencyDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public decimal? ExchangeRateToUSD { get; set; }
    public string ImageUrl { get; set; }
    public string LogoUrl { get; set; }
    public int? DecimalPlaces { get; set; }
    public decimal? MinimumTransactionAmount { get; set; }
    public decimal? MaximumTransactionAmount { get; set; }
    public string Network { get; set; }
    public string ContractAddress { get; set; }
    public decimal? NetworkFee { get; set; }
    public int? ConfirmationsRequired { get; set; }
    public bool? IsActive { get; set; }
}

public class CurrencyDetailsDto
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Type { get; set; }
    public decimal ExchangeRateToUSD { get; set; }
    public string ImageUrl { get; set; }
    public string LogoUrl { get; set; }
    public int DecimalPlaces { get; set; }
    public decimal MinimumTransactionAmount { get; set; }
    public decimal MaximumTransactionAmount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string Network { get; set; }
    public string ContractAddress { get; set; }
    public decimal NetworkFee { get; set; }
    public int ConfirmationsRequired { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public decimal Volume24h { get; set; } // 24h trading volume
    public decimal PercentChange24h { get; set; } // 24h price change
    public decimal MarketCap { get; set; } // For crypto
}

public class CryptoExchangeDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    public Guid? ToAccountId { get; set; }

    [Required]
    [Range(0.00000001, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string FromCurrency { get; set; }

    [Required]
    public string ToCurrency { get; set; }

    public string WalletAddress { get; set; } // For crypto withdrawals
    public string Network { get; set; } // Specify network for multi-chain cryptos
    public string Memo { get; set; } // For some cryptos like XRP, XLM
}

public class CryptoWalletDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CurrencyCode { get; set; }
    public string WalletAddress { get; set; }
    public string Network { get; set; }
    public decimal Balance { get; set; }
    public decimal PendingBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CryptoTransactionDto> RecentTransactions { get; set; }
}

public class CryptoTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionHash { get; set; }
    public string Type { get; set; } // Deposit, Withdrawal
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public decimal NetworkFee { get; set; }
    public int Confirmations { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class CurrencyStatisticsDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal Volume24h { get; set; }
    public decimal High24h { get; set; }
    public decimal Low24h { get; set; }
    public decimal PercentChange1h { get; set; }
    public decimal PercentChange24h { get; set; }
    public decimal PercentChange7d { get; set; }
    public decimal MarketCap { get; set; }
    public decimal CirculatingSupply { get; set; }
    public decimal TotalSupply { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CurrencyPriceHistoryDto
{
    public string Currency { get; set; }
    public List<PricePointDto> PriceHistory { get; set; }
}

public class PricePointDto
{
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
}

