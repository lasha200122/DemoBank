using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class BankingDetailsDto
    {
        public Guid? Id { get; set; } = null;
        public CardPaymentDetails? CardDetails { get; set; }
        public BankAccountDetails? BankDetails { get; set; }
        public IbanDetails? IbanDetails { get; set; }
        public CryptocurrencyDetails? CryptocurrencyDetails { get; set; }
    }
}
