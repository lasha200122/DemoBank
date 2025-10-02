using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models;

public class LoanPayment
{
    public Guid Id { get; set; }

    [Required]
    public Guid LoanId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public DateTime PaymentDate { get; set; }

    // Navigation property
    public virtual Loan Loan { get; set; }
}