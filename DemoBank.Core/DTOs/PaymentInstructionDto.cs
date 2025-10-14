using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class PaymentInstructionDto
    {
        public PaymentMethod Method { get; set; }

        // IBAN / Bank
        public string? BeneficialName { get; set; }
        public string? Iban { get; set; }
        public string? Reference { get; set; }
        public string? Bic { get; set; }

        // Crypto
        public string? WalletAddress { get; set; }
        public string? CryptoNetwork { get; set; }
        public string? CryptoMemoOrTag { get; set; }
        public decimal? CryptoAmount { get; set; }
    }
}
