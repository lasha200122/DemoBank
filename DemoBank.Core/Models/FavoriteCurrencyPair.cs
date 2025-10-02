using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models;

public class FavoriteCurrencyPair
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(3)]
    public string FromCurrency { get; set; }

    [Required]
    [MaxLength(3)]
    public string ToCurrency { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public virtual User User { get; set; }
}