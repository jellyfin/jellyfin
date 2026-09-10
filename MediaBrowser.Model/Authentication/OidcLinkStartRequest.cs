namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Request to start OpenID Connect account linking.
/// </summary>
public class OidcLinkStartRequest
{
    /// <summary>
    /// Gets or sets the relative return URL.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
