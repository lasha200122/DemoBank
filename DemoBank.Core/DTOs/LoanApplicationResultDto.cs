using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class LoanApplicationResultDto
{
    public bool Success { get; set; }
    public Guid LoanId { get; set; }
    public string Status { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal TotalRepayment { get; set; }
    public string Message { get; set; }
}