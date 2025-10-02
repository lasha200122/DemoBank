using DemoBank.Core.DTOs;
using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface IUserService
{
    Task<User> GetByIdAsync(Guid userId);
    Task<User> GetByUsernameAsync(string username);
    Task<User> GetByEmailAsync(string email);
    Task<User> CreateUserAsync(UserRegistrationDto registrationDto, UserRole role = UserRole.Client);
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}