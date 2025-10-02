using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class CurrencyDto
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public decimal ExchangeRateToUSD { get; set; }
    public DateTime LastUpdated { get; set; }
}