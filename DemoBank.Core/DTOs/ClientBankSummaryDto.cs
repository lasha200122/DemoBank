using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public sealed class ClientBankSummaryDto
    {
        public Guid ClientId { get; set; }
        public string FullName { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public int InvestmentRange { get; set; }
        public int Status { get; set; }
        public bool EmailStatus { get; set; }
        public string? Passkey { get; set; }
        public int ActiveAccounts { get; set; }
        public int ActiveInvestments { get; set; }
        public int ActiveLoans { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public decimal TotalBalanceUSD { get; set; }
        public decimal TotalBalanceEUR { get; set; }
        public decimal MonthlyReturnsUSD { get; set; }
        public decimal YearlyReturnsUSD { get; set; }
        public decimal MonthlyReturnsEUR { get; set; }
        public decimal YearlyReturnsEUR { get; set; }

    }

        //public sealed class BankingDetailsItemDto
        //{
        //    public Guid? UserId { get; set; }
        //    public BankAccountDetails BankDetails { get; set; }
        //    public CardPaymentDetails CardPaymentDetails { get; set; }
        //    public CryptocurrencyDetails CryptocurrencyDetails { get; set; }
        //    public IbanDetails IbanDetails { get; set; }
        //}

}
