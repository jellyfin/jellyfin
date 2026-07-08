namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Request to start OpenID Connect authentication.
/// </summary>
public class OidcStartRequest
{
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string App { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client version.
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative return URL.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
