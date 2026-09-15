using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using Jellyfin.Api.Constants;
using Jellyfin.Server.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests;

/// <summary>
/// Tests the claim actions applied to configured OpenID Connect providers.
/// </summary>
public class OidcClaimActionsTests
{
    private const string ProviderId = "authelia";

    /// <summary>
    /// OpenIdConnectOptions deletes the "iss" claim by default, and the handler runs the
    /// claim actions whether or not the UserInfo endpoint is used. External identities are
    /// keyed on issuer and subject, so the issuer claim has to survive.
    /// </summary>
    /// <param name="getClaimsFromUserInfoEndpoint">Whether the provider reads the UserInfo endpoint.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClaimActions_PreserveIssuerClaim(bool getClaimsFromUserInfoEndpoint)
    {
        var options = BuildOpenIdConnectOptions(getClaimsFromUserInfoEndpoint);

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("iss", "https://id.example.com"));
        identity.AddClaim(new Claim("sub", "user-1"));

        RunClaimActions(options, identity);

        Assert.Equal("https://id.example.com", identity.FindFirst("iss")?.Value);
        Assert.Equal("user-1", identity.FindFirst("sub")?.Value);
    }

    /// <summary>
    /// The default delete action for "iss" must not be present on a configured provider.
    /// </summary>
    [Fact]
    public void ClaimActions_DoNotDeleteIssuer()
    {
        var options = BuildOpenIdConnectOptions(true);

        Assert.DoesNotContain(options.ClaimActions, action => string.Equals(action.ClaimType, "iss", System.StringComparison.OrdinalIgnoreCase));
    }

    private static void RunClaimActions(OpenIdConnectOptions options, ClaimsIdentity identity)
    {
        // Mirrors what OpenIdConnectHandler does: the actions always run, over the
        // UserInfo payload when one is fetched and over an empty payload otherwise.
        using var payload = JsonDocument.Parse("{}");
        foreach (var action in options.ClaimActions)
        {
            action.Run(payload.RootElement, identity, "test");
        }
    }

    private static OpenIdConnectOptions BuildOpenIdConnectOptions(bool getClaimsFromUserInfoEndpoint)
    {
        var provider = new OidcProviderOptions
        {
            ProviderId = ProviderId,
            Enabled = true,
            Authority = "https://id.example.com",
            ClientId = "jellyfin",
            ClientSecret = "secret",
            GetClaimsFromUserInfoEndpoint = getClaimsFromUserInfoEndpoint
        };

        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetOptions())
            .Returns(new OidcOptions { Providers = new List<OidcProviderOptions> { provider } });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddCustomAuthentication(configurationManager.Object);

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(AuthenticationSchemes.GetOidcScheme(ProviderId));
    }
}
