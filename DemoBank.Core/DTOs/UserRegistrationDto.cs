using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class UserRegistrationDto
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Phone]
    public string PhoneNumber { get; set; }
}