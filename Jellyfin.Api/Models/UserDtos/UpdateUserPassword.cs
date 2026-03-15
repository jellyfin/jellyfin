using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.UserDtos;

/// <summary>
/// The update user password request body.
/// </summary>
public class UpdateUserPassword
{
    /// <summary>
    /// Gets or sets the current sha1-hashed password.
    /// </summary>
    [Obsolete("Use SessionPassword")]
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// Gets or sets a new plain text password for the requested user.
    /// </summary>
    [Required]
    public required string NewPassword { get; set; }

    /// <summary>
    /// Gets or sets the current plain text password for the user attached to the current session.
    /// Only required when updating password for the current session or other administrators.
    /// </summary>
    public string? SessionPassword { get; set; }

    /// <summary>
    /// Gets or sets the current plain text password.
    /// </summary>
    [Obsolete("Use SessionPassword")]
    public string? CurrentPw { get; set; }

    /// <summary>
    /// Gets or sets the new plain text password.
    /// </summary>
    [Obsolete("Use NewPassword")]
    public string? NewPw { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to reset the password.
    /// </summary>
    [Obsolete("See SessionPassword")]
    public bool ResetPassword { get; set; }
}
