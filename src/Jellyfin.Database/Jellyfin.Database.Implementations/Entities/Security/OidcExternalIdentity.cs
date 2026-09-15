using System;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Database.Implementations.Entities.Security;

/// <summary>
/// External OpenID Connect identity linked to a Jellyfin user.
/// </summary>
public class OidcExternalIdentity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the Jellyfin user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the provider id.
    /// </summary>
    [MaxLength(64)]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer.
    /// </summary>
    [MaxLength(512)]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    [MaxLength(512)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred username.
    /// </summary>
    [MaxLength(256)]
    public string? PreferredUsername { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [MaxLength(320)]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last login date.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the user.
    /// </summary>
    public User? User { get; set; }
}
