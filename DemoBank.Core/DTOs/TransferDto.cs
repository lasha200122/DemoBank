using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class TransferDto
{
    [Required]
    public Guid FromAccountId { get; set; }

    [Required]
    public string ToAccountNumber { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }
}