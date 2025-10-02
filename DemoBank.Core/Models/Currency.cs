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
    [MaxLength(3)]
    public string Code { get; set; } // USD, EUR, GBP, etc.

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    [Required]
    [MaxLength(5)]
    public string Symbol { get; set; }

    public decimal ExchangeRateToUSD { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsActive { get; set; } = true;
}