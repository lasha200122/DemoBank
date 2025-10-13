using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class CreateBankInvestmentDto
    {
        public Guid Id { get; set; }
        public decimal YearlyPercent { get; set; }
        public decimal MonthlyPercent { get; set; }
        public string AccountId { get; set; }
    }
}
