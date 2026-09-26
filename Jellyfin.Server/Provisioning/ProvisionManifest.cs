using System.Collections.Generic;

namespace Jellyfin.Server.Provisioning;

/// <summary>
/// The contents of the file passed to <c>--provision-file</c>, describing the state a fresh
/// server should be brought up in by <see cref="Configuration.StartupMode.Provision"/>.
/// </summary>
/// <remarks>
/// Every member except <see cref="Administrator"/> is optional. An omitted member leaves the
/// server default alone rather than resetting it to empty, so a manifest only has to name the
/// settings the operator actually cares about.
/// </remarks>
public sealed class ProvisionManifest
{
    /// <summary>
    /// Gets or sets the administrator account to create.
    /// </summary>
    public required ProvisionAdministrator Administrator { get; set; }

    /// <summary>
    /// Gets or sets the friendly name the server reports to clients.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Gets or sets the culture used for the server's own UI strings, for example <c>en-US</c>.
    /// </summary>
    public string? UICulture { get; set; }

    /// <summary>
    /// Gets or sets the two letter country code used when fetching metadata, for example <c>US</c>.
    /// </summary>
    public string? MetadataCountryCode { get; set; }

    /// <summary>
    /// Gets or sets the preferred metadata language, for example <c>en</c>.
    /// </summary>
    public string? PreferredMetadataLanguage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server accepts connections from outside the local network.
    /// </summary>
    public bool? EnableRemoteAccess { get; set; }

    /// <summary>
    /// Gets or sets the libraries to create.
    /// </summary>
    public IReadOnlyList<ProvisionLibrary> Libraries { get; set; } = [];
}
