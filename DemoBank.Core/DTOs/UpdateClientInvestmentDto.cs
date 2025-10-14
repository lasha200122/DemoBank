using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class UpdateClientInvestmentDto
    {
        public string AccountId { get; set; }
        public decimal MonthlyReturn { get; set; }
        public decimal YearlyReturn { get; set; }
        public Guid UserId { get; set; }
    }
}
