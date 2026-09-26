using System.ComponentModel.DataAnnotations;

namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Request to create an explicit OpenID Connect external identity link.
/// </summary>
public class OidcExternalIdentityCreateRequest
{
    /// <summary>
    /// Gets or sets the provider id.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    [Required]
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
}
