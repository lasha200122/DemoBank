using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class LoanPaymentResultDto
{
    public bool Success { get; set; }
    public Guid PaymentId { get; set; }
    public Guid LoanId { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public string LoanStatus { get; set; }
    public string Message { get; set; }
}

public class LoanPaymentHistoryDto
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class LoanSummaryDto
{
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public decimal TotalBorrowed { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal MonthlyPaymentsDue { get; set; }
    public DateTime? NextPaymentDate { get; set; }
}

public class LoanEligibilityDto
{
    public bool IsEligible { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal MaxEligibleAmount { get; set; }
    public int CreditScore { get; set; }
    public List<string> Reasons { get; set; }
}

public class SetAutoPaymentDto
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public bool Enable { get; set; }
}