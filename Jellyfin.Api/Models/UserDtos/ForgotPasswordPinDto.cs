using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.UserDtos;

/// <summary>
/// Forgot Password Pin enter request body DTO.
/// </summary>
public class ForgotPasswordPinDto
{
    /// <summary>
    /// Gets or sets the entered pin to have the password reset.
    /// </summary>
    [Required]
    public required string Pin { get; set; }
}
