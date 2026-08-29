using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Dto;

/// <summary>DTO for registering a new account via api/auth/register.</summary>
public class RegisterDto
{
    /// <summary>Email of the account to create.</summary>
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>Account password. Required; validated against ASP.NET Core Identity's password policy.</summary>
    [Required]
    public required string Password { get; set; }
}