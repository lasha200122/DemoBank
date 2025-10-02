namespace DemoBank.Core.DTOs;

public class RecentRecipientDto
{
    public string AccountNumber { get; set; }
    public string Name { get; set; }
    public DateTime LastTransferDate { get; set; }
    public int TransferCount { get; set; }
}