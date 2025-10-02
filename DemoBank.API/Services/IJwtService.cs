using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    Guid? ValidateToken(string token);
}