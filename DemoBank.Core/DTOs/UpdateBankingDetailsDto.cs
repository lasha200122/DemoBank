using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs
{
    public class UpdateBankingDetailsDto : CreateBankingDetailsDto
    {
        public Guid Id { get; set; }
        public BankingDetailsDto BankingDetails { get; set; }
    }
}
