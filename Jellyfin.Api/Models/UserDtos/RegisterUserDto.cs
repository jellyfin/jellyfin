using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.UserDtos;

/// <summary>
/// The initial user registration request body.
/// </summary>
public class RegisterUserDto
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [MinLength(1)]
    [MaxLength(255)]
    [RegularExpression(@"^[\w'._@+-]+$", ErrorMessage = "Username must contain only letters, numbers, and certain special characters.")]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the password. Required because the first user will be an administrator.
    /// </summary>
    [MinLength(1)]
    [MaxLength(255)]
    public required string Password { get; set; }
}
