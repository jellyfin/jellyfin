namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Request to exchange an OpenID Connect one-time code for a Jellyfin authentication result.
/// </summary>
public class OidcExchangeRequest
{
    /// <summary>
    /// Gets or sets the one-time exchange code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
