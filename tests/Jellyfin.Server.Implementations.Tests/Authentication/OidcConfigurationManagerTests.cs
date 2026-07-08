using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Authentication;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Authentication;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Authentication;

public class OidcConfigurationManagerTests
{
    [Fact]
    public async Task GetConfiguration_WhenSecretStored_RedactsSecret()
    {
        var (manager, tempDirectory) = CreateManager();
        try
        {
            var update = new OidcConfigurationUpdateDto
            {
                Providers = [CreateValidProvider("stored-secret")]
            };
            await manager.UpdateConfigurationAsync(update, CancellationToken.None);

            var configuration = manager.GetConfiguration();
            var provider = Assert.Single(configuration.Providers);

            Assert.True(provider.HasClientSecret);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateConfiguration_WhenClientSecretMissing_PreservesExistingSecret(string? clientSecret)
    {
        var (manager, tempDirectory) = CreateManager();
        try
        {
            var update = new OidcConfigurationUpdateDto
            {
                Providers = [CreateValidProvider("stored-secret")]
            };
            await manager.UpdateConfigurationAsync(update, CancellationToken.None);

            var provider = CreateValidProvider(clientSecret);
            provider.Name = "Updated";

            var updated = new OidcConfigurationUpdateDto
            {
                Providers = [provider]
            };
            await manager.UpdateConfigurationAsync(updated, CancellationToken.None);

            var storedProvider = Assert.Single(manager.GetOptions().Providers);
            Assert.Equal("stored-secret", storedProvider.ClientSecret);
            Assert.Equal("Updated", storedProvider.Name);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task UpdateConfiguration_WhenInsecureAuthorityExplicitlyAllowed_Succeeds()
    {
        var (manager, tempDirectory) = CreateManager();
        try
        {
            var provider = CreateValidProvider();
            provider.Authority = "http://auth.example.com";
            provider.AllowInsecureAuthority = true;

            var update = new OidcConfigurationUpdateDto
            {
                Providers = [provider]
            };
            await manager.UpdateConfigurationAsync(update, CancellationToken.None);

            var storedProvider = Assert.Single(manager.GetOptions().Providers);
            Assert.Equal("http://auth.example.com", storedProvider.Authority);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Theory]
    [MemberData(nameof(InvalidEnabledProviderData))]
    public async Task UpdateConfiguration_WhenEnabledProviderInvalid_ThrowsArgumentException(
        OidcProviderConfigurationUpdateDto provider,
        string expectedMessage)
    {
        var (manager, tempDirectory) = CreateManager();
        try
        {
            var update = new OidcConfigurationUpdateDto
            {
                Providers = [provider]
            };
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => manager.UpdateConfigurationAsync(update, CancellationToken.None));

            Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    public static TheoryData<OidcProviderConfigurationUpdateDto, string> InvalidEnabledProviderData()
    {
        var missingProviderId = CreateValidProvider();
        missingProviderId.ProviderId = string.Empty;

        var invalidProviderId = CreateValidProvider();
        invalidProviderId.ProviderId = "authelia/default";

        var missingAuthority = CreateValidProvider();
        missingAuthority.Authority = string.Empty;

        var insecureAuthority = CreateValidProvider();
        insecureAuthority.Authority = "http://auth.example.com";

        var invalidAuthorityScheme = CreateValidProvider();
        invalidAuthorityScheme.Authority = "ftp://auth.example.com";
        invalidAuthorityScheme.AllowInsecureAuthority = true;

        var missingClientId = CreateValidProvider();
        missingClientId.ClientId = string.Empty;

        var missingClientSecret = CreateValidProvider();
        missingClientSecret.ClientSecret = string.Empty;

        var missingOpenIdScope = CreateValidProvider();
        missingOpenIdScope.Scopes = ["profile", "email"];

        return new TheoryData<OidcProviderConfigurationUpdateDto, string>
        {
            { missingProviderId, "provider id" },
            { invalidProviderId, "invalid" },
            { missingAuthority, "authority" },
            { insecureAuthority, "HTTPS" },
            { invalidAuthorityScheme, "HTTP or HTTPS" },
            { missingClientId, "client id" },
            { missingClientSecret, "client secret" },
            { missingOpenIdScope, "openid" }
        };
    }

    private static OidcProviderConfigurationUpdateDto CreateValidProvider(string? clientSecret = "client-secret")
    {
        return new OidcProviderConfigurationUpdateDto
        {
            Enabled = true,
            ProviderId = "authelia",
            Name = "Authelia",
            Authority = "https://auth.example.com",
            ClientId = "jellyfin",
            ClientSecret = clientSecret,
            Scopes = ["openid", "profile", "email", "groups"],
            UsernameClaim = "preferred_username",
            RoleClaim = "groups",
            EmailClaim = "email",
            RequiredGroups = ["jellyfin"],
            AdminGroups = ["jellyfin-admins"],
            ProvisioningMode = OidcUserProvisioningMode.Disabled,
            GetClaimsFromUserInfoEndpoint = true,
            EnableDeviceAuthorization = true,
            EnableRpInitiatedLogout = true
        };
    }

    private static (OidcConfigurationManager Manager, string TempDirectory) CreateManager()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "jellyfin-oidc-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);

        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.Setup(paths => paths.ConfigurationDirectoryPath).Returns(tempDirectory);

        return (new OidcConfigurationManager(applicationPaths.Object), tempDirectory);
    }
}
