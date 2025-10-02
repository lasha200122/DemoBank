using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class UserSettingsDto
{
    public string PreferredCurrency { get; set; }
    public string Language { get; set; }
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public decimal DailyTransferLimit { get; set; }
    public decimal DailyWithdrawalLimit { get; set; }
}