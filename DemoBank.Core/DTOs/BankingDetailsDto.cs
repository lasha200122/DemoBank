using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class BankingDetailsDto : AdminClientListDto
    {
        public Guid? UserId { get; set; }
        [Required]
        public string BeneficialName { get; set; }

        [Required]
        public string IBAN { get; set; }

        [Required]
        public string Reference { get; set; }

        [Required]
        public string BIC { get; set; }
    }
}
