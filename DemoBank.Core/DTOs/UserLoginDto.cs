using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class UserLoginDto
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }
}