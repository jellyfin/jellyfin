using System;

namespace Jellyfin.Api.Models.ApiKeyDtos;

/// <summary>
/// An API key.
/// </summary>
public class AuthenticationInfoDto
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the device identifier.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the name of the app using the key.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// Gets or sets the name of the device.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date the key was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Gets or sets the date the key was revoked.
    /// </summary>
    public DateTime? DateRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date of the last activity.
    /// </summary>
    public DateTime DateLastActivity { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string? UserName { get; set; }
}
