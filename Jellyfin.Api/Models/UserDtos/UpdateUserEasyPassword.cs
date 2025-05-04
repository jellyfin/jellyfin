namespace Jellyfin.Api.Models.UserDtos;

/// <summary>
/// The update user easy password request body.
/// </summary>
public class UpdateUserEasyPassword
{
    /// <summary>
    /// Gets or sets the new sha1-hashed password.
    /// </summary>
    public string? NewPassword { get; set; }

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    public string? NewPw { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to reset the password.
    /// </summary>
    public bool ResetPassword { get; set; }
}
