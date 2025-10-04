using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models;

public class Currency
{
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } // USD, EUR, BTC, ETH, etc.

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; }

    public CurrencyType Type { get; set; } // Fiat or Crypto

    public decimal ExchangeRateToUSD { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } // Currency icon/image URL

    [MaxLength(500)]
    public string LogoUrl { get; set; } // Full logo URL for display

    public int DecimalPlaces { get; set; } // 2 for fiat, 8 for most crypto

    public decimal MinimumTransactionAmount { get; set; }
    public decimal MaximumTransactionAmount { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;

    [MaxLength(100)]
    public string Network { get; set; } // For crypto: Bitcoin, Ethereum, BSC, etc.

    [MaxLength(100)]
    public string ContractAddress { get; set; } // For tokens

    public decimal NetworkFee { get; set; } // Typical network fee for crypto
    public int ConfirmationsRequired { get; set; } // For crypto transactions

    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }

    [MaxLength(50)]
    public string CreatedBy { get; set; }

    [MaxLength(50)]
    public string UpdatedBy { get; set; }
}

public enum CurrencyType
{
    Fiat,
    Crypto
}