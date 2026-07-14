using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Models;

/// <summary>
/// Data Transfer Object used to authenticate a user via api/auth/login.
/// Exchanged for a JWT access token by <see cref="Controllers.AuthController.Login"/>.
/// </summary>
public class LoginDto
{
    /// <summary>Email of the account to sign in as. Required (validated by <see cref="RequiredAttribute"/> and <see cref="EmailAddressAttribute"/>).</summary>
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>Account password. Required.</summary>
    [Required]
    public required string Password { get; set; }
}