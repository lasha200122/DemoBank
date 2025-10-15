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
    [Range(50000, 1_000_000)]
    public decimal Amount { get; set; }

    [Required]
    [Range(6, 72)]
    public int TermMonths { get; set; }

    [MaxLength(500)]
    public string Purpose { get; set; }
}