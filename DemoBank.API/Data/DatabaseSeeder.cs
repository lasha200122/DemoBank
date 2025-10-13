using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Data;

public class DatabaseSeeder
{
    private readonly DemoBankContext _context;

    public DatabaseSeeder(DemoBankContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Seed admin user
        await SeedAdminUserAsync();

        // Seed test client users
        await SeedTestClientsAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        // Check if admin already exists
        if (await _context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@demobank.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var adminSettings = new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            PreferredCurrency = "USD",
            Language = "en",
            EmailNotifications = true,
            SmsNotifications = false,
            TwoFactorEnabled = false,
            DailyTransferLimit = 1000000,
            DailyWithdrawalLimit = 500000,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        _context.UserSettings.Add(adminSettings);

        // Create initial notification for admin
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            Title = "Welcome Admin",
            Message = "Admin account has been created. Default password is 'Admin123!' - Please change it immediately.",
            Type = NotificationType.Security,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        Console.WriteLine("Admin user seeded successfully:");
        Console.WriteLine("Username: admin");
        Console.WriteLine("Password: Admin123!");
    }

    private async Task SeedTestClientsAsync()
    {
        // Check if test users already exist
        if (await _context.Users.AnyAsync(u => u.Email == "john.doe@example.com"))
            return;

        // Create test client 1
        var client1 = new User
        {
            Id = Guid.NewGuid(),
            Username = "johndoe",
            Email = "john.doe@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Client,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var client1Settings = new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = client1.Id,
            PreferredCurrency = "USD",
            Language = "en",
            EmailNotifications = true,
            SmsNotifications = false,
            TwoFactorEnabled = false,
            DailyTransferLimit = 10000,
            DailyWithdrawalLimit = 5000,
            CreatedAt = DateTime.UtcNow
        };

        // Create checking account for client 1
        var checkingAccount1 = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1234567890",
            UserId = client1.Id,
            Type = AccountType.Checking,
            Currency = "USD",
            Balance = 5000, // Initial balance
            IsPriority = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Create savings account for client 1
        var savingsAccount1 = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1234567891",
            UserId = client1.Id,
            Type = AccountType.Savings,
            Currency = "USD",
            Balance = 10000, // Initial balance
            IsPriority = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Create test client 2
        var client2 = new User
        {
            Id = Guid.NewGuid(),
            Username = "janesmith",
            Email = "jane.smith@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FirstName = "Jane",
            LastName = "Smith",
            Role = UserRole.Client,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var client2Settings = new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = client2.Id,
            PreferredCurrency = "EUR",
            Language = "en",
            EmailNotifications = true,
            SmsNotifications = true,
            TwoFactorEnabled = false,
            DailyTransferLimit = 15000,
            DailyWithdrawalLimit = 7500,
            CreatedAt = DateTime.UtcNow
        };

        // Create checking account for client 2
        var checkingAccount2 = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "9876543210",
            UserId = client2.Id,
            Type = AccountType.Checking,
            Currency = "USD",
            Balance = 7500,
            IsPriority = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Create EUR account for client 2
        var eurAccount = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "9876543211",
            UserId = client2.Id,
            Type = AccountType.Checking,
            Currency = "EUR",
            Balance = 2000,
            IsPriority = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Add all entities
        _context.Users.AddRange(client1, client2);
        _context.UserSettings.AddRange(client1Settings, client2Settings);
        _context.Accounts.AddRange(checkingAccount1, savingsAccount1, checkingAccount2, eurAccount);

        // Create some sample transactions
        var transactions = new[]
        {
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = checkingAccount1.Id,
                    Type = TransactionType.Deposit,
                    Amount = 5000,
                    Currency = "USD",
                    AmountInAccountCurrency = 5000,
                    Description = "Initial deposit",
                    BalanceAfter = 5000,
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = savingsAccount1.Id,
                    Type = TransactionType.Deposit,
                    Amount = 10000,
                    Currency = "USD",
                    AmountInAccountCurrency = 10000,
                    Description = "Initial deposit",
                    BalanceAfter = 10000,
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = checkingAccount2.Id,
                    Type = TransactionType.Deposit,
                    Amount = 7500,
                    Currency = "USD",
                    AmountInAccountCurrency = 7500,
                    Description = "Initial deposit",
                    BalanceAfter = 7500,
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                }
            };

        _context.Transactions.AddRange(transactions);

        await _context.SaveChangesAsync();

        Console.WriteLine("\nTest client users seeded successfully:");
        Console.WriteLine("Client 1 - Username: johndoe, Password: Test123!");
        Console.WriteLine("Client 2 - Username: janesmith, Password: Test123!");
    }
}

public static class DatabaseSeederExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoBankContext>();
        var seeder = new DatabaseSeeder(context);
        await seeder.SeedAsync();
    }
}