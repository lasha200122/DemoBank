using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class LoanDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
    public string Status { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string BorrowerName { get; set; }
}