using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class UserService : IUserService
{
    private readonly DemoBankContext _context;
    private readonly INotificationHelper _notificationHelper;

    public UserService(DemoBankContext context, INotificationHelper notificationHelper)
    {
        _context = context;
        _notificationHelper = notificationHelper;
    }

    public async Task<User> GetByIdAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User> CreateUserAsync(UserRegistrationDto registrationDto, UserRole role = UserRole.Client)
    {
        // Check if username or email already exists
        if (await UsernameExistsAsync(registrationDto.Username))
            throw new InvalidOperationException("Username already exists");

        if (await EmailExistsAsync(registrationDto.Email))
            throw new InvalidOperationException("Email already exists");

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = registrationDto.Username,
            Email = registrationDto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registrationDto.Password),
            FirstName = registrationDto.FirstName,
            LastName = registrationDto.LastName,
            Role = role,
            Status = Status.Active,
            CreatedAt = DateTime.UtcNow,
            PotentialInvestmentRange = registrationDto.PotentialInvestmentRange.Value
        };

        // Create user settings
        var settings = new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreferredCurrency = "USD",
            Language = "en",
            EmailNotifications = true,
            SmsNotifications = false,
            TwoFactorEnabled = false,
            DailyTransferLimit = 10000,
            DailyWithdrawalLimit = 5000,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.UserSettings.Add(settings);

        // Create default checking account for clients
        if (role == UserRole.Client)
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                AccountNumber = AccountNumberGenerator.GenerateAccountNumber(),
                UserId = user.Id,
                Type = AccountType.Checking,
                Currency = "USD",
                Balance = 0,
                IsPriority = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);

            // Send welcome notification
            await _notificationHelper.CreateNotification(
                user.Id,
                "Welcome to DemoBank!",
                $"Hello {user.FirstName}, welcome to DemoBank! Your account has been created successfully. Your default checking account number is {account.AccountNumber}.",
                NotificationType.Success
            );
        }
        else
        {
            // Admin welcome notification
            await _notificationHelper.CreateNotification(
                user.Id,
                "Admin Account Created",
                $"Welcome {user.FirstName}, your admin account has been created successfully.",
                NotificationType.Success
            );
        }

        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ValidatePasswordAsync(User user, string password)
    {
        if (user == null || string.IsNullOrEmpty(password))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await GetByIdAsync(userId);
        if (user == null)
            return false;

        // Validate current password
        if (!await ValidatePasswordAsync(user, currentPassword))
            return false;

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);

        // Send notification
        await _notificationHelper.CreateNotification(
            userId,
            "Password Changed",
            "Your password has been changed successfully. If this wasn't you, please contact support immediately.",
            NotificationType.Security
        );

        return await _context.SaveChangesAsync() > 0;
    }
}