using System.Collections.Generic;

namespace MediaBrowser.Model.Authentication;

/// <summary>
/// OpenID Connect provider options.
/// </summary>
public class OidcProviderOptions : OidcProviderConfigurationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OidcProviderOptions"/> class.
    /// </summary>
    public OidcProviderOptions()
    {
        ProviderId = "authelia";
        Name = "Authelia";
        Scopes = new List<string> { "openid", "profile", "email", "groups" };
        UsernameClaim = "preferred_username";
        RoleClaim = "groups";
        EmailClaim = "email";
        GetClaimsFromUserInfoEndpoint = true;
    }

    /// <summary>
    /// Gets or sets the client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}
