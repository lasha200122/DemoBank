using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class StockDto
{
    public string Symbol { get; set; }
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public decimal? Price { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class StockSearchResultDto
{
    public List<StockDto> Stocks { get; set; }
    public int TotalResults { get; set; }
}