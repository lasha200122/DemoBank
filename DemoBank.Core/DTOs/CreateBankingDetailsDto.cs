using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class CreateBankingDetailsDto
    {
        public Guid? UserId { get; set; }
        public BankingDetailsDto? BankingDetails { get; set; }
    }

    public class CardPaymentDetails
    {
        [CreditCard]
        public string CardNumber { get; set; }

        [MaxLength(100)]
        public string CardHolderName { get; set; }

        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$")]
        public string ExpiryDate { get; set; } // MM/YY format

        [RegularExpression(@"^\d{3,4}$")]
        public string CVV { get; set; }
    }

    public class BankAccountDetails
    {
        [Required]
        public string AccountNumber { get; set; }

        [Required]
        public string RoutingNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string AccountHolderName { get; set; }

    }

    public class CryptocurrencyDetails
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(@"^(0x[a-fA-F0-9]{40}|[13][a-km-zA-HJ-NP-Z1-9]{25,34}|bc1[a-zA-HJ-NP-Z0-9]{11,71})$",
            ErrorMessage = "Invalid wallet address format.")]
        public string WalletAddress { get; set; }

        [MaxLength(100)]
        [RegularExpression(@"^[A-Fa-f0-9]{64}$",
            ErrorMessage = "Invalid transaction hash format (must be a 64-character hex string).")]
        public string TransactionHash { get; set; }
    }

    public class IbanDetails
    {
        [Required]
        [MaxLength(100)]
        public string BeneficialName { get; set; }

        [Required]
        [MaxLength(34)]
        [RegularExpression(@"^[A-Z]{2}\d{2}[A-Z0-9]{1,30}$", ErrorMessage = "Invalid IBAN format.")]
        public string IBAN { get; set; }

        [MaxLength(100)]
        public string Reference { get; set; }

        [MaxLength(11)]
        [RegularExpression(@"^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$", ErrorMessage = "Invalid BIC/SWIFT code.")]
        public string BIC { get; set; }
    }
}
