using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Database.Implementations.Entities.Security;

/// <summary>
/// OpenID Connect metadata associated with a Jellyfin device token.
/// </summary>
public class OidcSession
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the Jellyfin access token.
    /// </summary>
    [MaxLength(256)]
    public string AccessToken { get; set; } = string.Empty;

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
    /// Gets or sets the upstream session id.
    /// </summary>
    [MaxLength(256)]
    public string? Sid { get; set; }

    /// <summary>
    /// Gets or sets the protected id token hint used for RP-initiated logout.
    /// </summary>
    public string? ProtectedIdTokenHint { get; set; }

    /// <summary>
    /// Gets or sets the upstream session id compatibility alias.
    /// </summary>
    [NotMapped]
    public string? SessionId
    {
        get => Sid;
        set => Sid = value;
    }

    /// <summary>
    /// Gets or sets the protected id token hint compatibility alias.
    /// </summary>
    [NotMapped]
    public string? IdTokenHint
    {
        get => ProtectedIdTokenHint;
        set => ProtectedIdTokenHint = value;
    }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
