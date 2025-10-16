using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class TopUpRequestCreatedDto
    {
        public Guid TransactionId { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentInstructionDto? PaymentInstruction { get; set; }
    }
}
