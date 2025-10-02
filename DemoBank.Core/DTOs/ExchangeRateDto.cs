using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class ExchangeRateDto
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTime Timestamp { get; set; }
}
