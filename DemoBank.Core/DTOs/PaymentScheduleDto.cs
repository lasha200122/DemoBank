using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class PaymentScheduleDto
{
    public Guid LoanId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public List<PaymentScheduleItemDto> Payments { get; set; }
}

public class PaymentScheduleItemDto
{
    public int PaymentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string Status { get; set; }
    public DateTime? PaidDate { get; set; }
}