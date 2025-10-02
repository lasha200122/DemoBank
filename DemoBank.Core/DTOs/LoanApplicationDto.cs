using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class LoanApplicationDto
{
    [Required]
    [Range(1000, 100000)]
    public decimal Amount { get; set; }

    [Required]
    [Range(6, 60)]
    public int TermMonths { get; set; }

    [MaxLength(500)]
    public string Purpose { get; set; }
}