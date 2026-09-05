namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Result of starting OpenID Connect authentication.
/// </summary>
public class OidcStartResult
{
    /// <summary>
    /// Gets or sets the browser start URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
