namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Result containing an OpenID Connect one-time exchange code.
/// </summary>
public class OidcExchangeResult
{
    /// <summary>
    /// Gets or sets the one-time exchange code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
