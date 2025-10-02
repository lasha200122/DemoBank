using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue { get; set; }
}
