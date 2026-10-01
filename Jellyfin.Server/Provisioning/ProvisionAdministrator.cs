namespace Jellyfin.Server.Provisioning;

/// <summary>
/// The initial administrator account described by a <see cref="ProvisionManifest"/>.
/// </summary>
public sealed class ProvisionAdministrator
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public required string Password { get; set; }
}
