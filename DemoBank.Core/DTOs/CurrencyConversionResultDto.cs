using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class CurrencyConversionResultDto
{
    public decimal OriginalAmount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
    public DateTime Timestamp { get; set; }
}
