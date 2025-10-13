using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models
{
    public class ClientInvestmentResponse
    {
        public Guid? Id { get; set; }
        public Guid UserId { get; set; }
        public decimal MonthlyReturn { get; set; }
        public decimal  YearlyReturn { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AccountId { get; set; }
    }
}
