using System.Collections.Generic;

namespace MediaBrowser.Model.Authentication;

/// <summary>
/// OpenID Connect provider configuration update DTO.
/// </summary>
public class OidcProviderConfigurationUpdateDto
{
    /// <summary>
    /// Gets or sets a value indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the stable provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenID Connect authority URL.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret. Null or empty values preserve the current stored secret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HTTP authority metadata is allowed.
    /// </summary>
    public bool AllowInsecureAuthority { get; set; }

    /// <summary>
    /// Gets or sets the requested scopes.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the username claim.
    /// </summary>
    public string UsernameClaim { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role/group claim.
    /// </summary>
    public string RoleClaim { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email claim.
    /// </summary>
    public string EmailClaim { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets groups required for login. Empty means no group gate.
    /// </summary>
    public IReadOnlyList<string> RequiredGroups { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets groups that grant administrator policy to newly created users.
    /// </summary>
    public IReadOnlyList<string> AdminGroups { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the provisioning mode.
    /// </summary>
    public OidcUserProvisioningMode ProvisioningMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the administrator role should be synchronized on every login.
    /// </summary>
    public bool SyncAdminRole { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether claims should be retrieved from the UserInfo endpoint.
    /// </summary>
    public bool GetClaimsFromUserInfoEndpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether device authorization flow endpoints are enabled for this provider.
    /// </summary>
    public bool EnableDeviceAuthorization { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether RP-initiated logout should be attempted when supported by the provider.
    /// </summary>
    public bool EnableRpInitiatedLogout { get; set; }
}
