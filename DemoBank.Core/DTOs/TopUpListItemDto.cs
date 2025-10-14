using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class TopUpListItemDto
    {
        public Guid TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid AccountId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Status { get; set; } // Pending/Completed/Rejected
    }
}
