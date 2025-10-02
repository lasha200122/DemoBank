namespace DemoBank.API.Helpers;

public static class AccountNumberGenerator
{
    private static readonly Random _random = new Random();

    public static string GenerateAccountNumber()
    {
        // Generate a 10-digit account number
        const string digits = "0123456789";
        return new string(Enumerable.Repeat(digits, 10)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public static string GenerateInvoiceNumber()
    {
        // Generate invoice number with format: INV-YYYYMMDD-XXXX
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = _random.Next(1000, 9999);
        return $"INV-{date}-{random}";
    }
}