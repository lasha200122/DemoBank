using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class LoanApprovalResultDto
{
    public bool Success { get; set; }
    public Guid LoanId { get; set; }
    public string Status { get; set; }
    public decimal DisbursedAmount { get; set; }
    public string DisbursementAccount { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime FirstPaymentDate { get; set; }
    public string Message { get; set; }
}
