using System;

namespace MediaBrowser.Model.Authentication;

/// <summary>
/// External identity linked to a Jellyfin user.
/// </summary>
public class ExternalIdentityDto
{
    /// <summary>
    /// Gets or sets the identity id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the linked Jellyfin user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the provider id.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred username.
    /// </summary>
    public string? PreferredUsername { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last login date.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
