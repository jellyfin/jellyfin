namespace Jellyfin.Api.Models.UserDtos;

/// <summary>
/// The authenticate user by name request body.
/// </summary>
public class AuthenticateUserByName
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the plain text password.
    /// </summary>
    public string? Pw { get; set; }
}
