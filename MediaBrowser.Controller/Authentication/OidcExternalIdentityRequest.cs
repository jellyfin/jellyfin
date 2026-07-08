using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Authentication;

/// <summary>
/// OpenID Connect external identity claims used for Jellyfin sign-in.
/// </summary>
public class OidcExternalIdentityRequest
{
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
    /// Gets or sets the groups.
    /// </summary>
    public IReadOnlyList<string> Groups { get; set; } = Array.Empty<string>();

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
    /// Gets or sets the remote endpoint.
    /// </summary>
    public string RemoteEndPoint { get; set; } = string.Empty;
}
