namespace DemoBank.Core.DTOs;

public class LoanApprovalDto
{
    public Guid? DisbursementAccountId { get; set; }
    public decimal? OverrideInterestRate { get; set; }
    public string Notes { get; set; }
}