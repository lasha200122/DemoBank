using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models;

public class ExchangeRateHistory
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(3)]
    public string FromCurrency { get; set; }

    [Required]
    [MaxLength(3)]
    public string ToCurrency { get; set; }

    public decimal Rate { get; set; }
    public DateTime RecordedAt { get; set; }
}