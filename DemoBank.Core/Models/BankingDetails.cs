using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.Models
{
    public class BankingDetails
    {
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public virtual User User { get; set; }

        // IBAN fields
        [MaxLength(100)]
        public string? BeneficialName { get; set; }

        [MaxLength(34)]
        public string? IBAN { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; }

        [MaxLength(12)]
        public string? BIC { get; set; }

        // Card fields
        [MaxLength(20)]
        public string? CardNumber { get; set; }

        [MaxLength(100)]
        public string? CardHolderName { get; set; }

        [MaxLength(5)]
        public string? ExpiryDate { get; set; }

        [MaxLength(4)]
        public string? CVV { get; set; }

        // Crypto fields
        [MaxLength(100)]
        public string? WalletAddress { get; set; }

        [MaxLength(100)]
        public string? TransactionHash { get; set; }

        // Bank Account fields
        [MaxLength(34)]
        public string? AccountNumber { get; set; }

        [MaxLength(20)]
        public string? RoutingNumber { get; set; }

        [MaxLength(100)]
        public string? AccountHolderName { get; set; }
    }
}
