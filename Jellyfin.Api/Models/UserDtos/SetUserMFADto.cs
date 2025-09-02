using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.UserDtos;

/// <summary>
/// Set user MFA request body DTO.
/// </summary>
public class SetUserMFADto
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to enable MFA for this user.
    /// </summary>
    [Required]
    public required bool Enable { get; set; }
}
