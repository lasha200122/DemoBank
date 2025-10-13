using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models
{
    public class BankingDetails
    {
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string BeneficialName { get; set; }

        [Required, MaxLength(34)]
        public string IBAN { get; set; }

        [Required, MaxLength(50)]
        public string Reference { get; set; }

        [Required, MaxLength(12)]
        public string BIC { get; set; }

        public Guid UserId { get; set; }

        public virtual User User { get; set; }
    }

}
