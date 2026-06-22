using System;

namespace Jellyfin.Api.Models.ApiKeyDtos;

/// <summary>
/// An API key.
/// </summary>
public class AuthenticationInfoDto
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the name of the app using the key.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the date the key was created.
    /// </summary>
    public DateTime DateCreated { get; set; }
}
