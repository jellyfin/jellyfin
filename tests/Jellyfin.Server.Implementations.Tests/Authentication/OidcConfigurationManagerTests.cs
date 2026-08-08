using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
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

    [Fact]
    public async Task GetConfiguration_WhenSavedConfigDiffersFromStartupSnapshot_ReturnsRequiresRestartAndActiveProvidersStayUnchanged()
    {
        var (manager, tempDirectory) = CreateManager(new OidcOptions
        {
            Providers = [CreateValidProviderOptions()]
        });

        try
        {
            var updatedProvider = CreateValidProvider("updated-secret");
            updatedProvider.Name = "Updated";
            updatedProvider.Authority = "https://updated.example.com";

            await manager.UpdateConfigurationAsync(
                new OidcConfigurationUpdateDto
                {
                    Providers = [updatedProvider]
                },
                CancellationToken.None);

            var configuration = manager.GetConfiguration();
            var configuredProvider = Assert.Single(configuration.Providers);
            Assert.True(configuration.RequiresRestart);
            Assert.Equal("Updated", configuredProvider.Name);
            Assert.Equal("https://updated.example.com", configuredProvider.Authority);

            var activeProvider = manager.GetEnabledProvider("authelia");
            Assert.NotNull(activeProvider);
            Assert.Equal("Authelia", activeProvider.Name);
            Assert.Equal("https://auth.example.com", activeProvider.Authority);

            activeProvider.Name = "Mutated";
            var activeProviderAfterMutation = manager.GetEnabledProvider("authelia");
            Assert.NotNull(activeProviderAfterMutation);
            Assert.Equal("Authelia", activeProviderAfterMutation.Name);

            var providerInfo = Assert.Single(manager.GetProviderInfos());
            Assert.Equal("Authelia", providerInfo.Name);
            Assert.Equal("https://auth.example.com", providerInfo.Authority);
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

    [Fact]
    public async Task UpdateConfiguration_WhenListPropertiesNull_NormalizesLists()
    {
        var (manager, tempDirectory) = CreateManager();
        try
        {
            var provider = CreateValidProvider();
            provider.Scopes = null!;
            provider.RequiredGroups = null!;
            provider.AdminGroups = null!;

            await manager.UpdateConfigurationAsync(
                new OidcConfigurationUpdateDto
                {
                    Providers = [provider]
                },
                CancellationToken.None);

            var storedProvider = Assert.Single(manager.GetOptions().Providers);
            Assert.Contains("openid", storedProvider.Scopes);
            Assert.Empty(storedProvider.RequiredGroups);
            Assert.Empty(storedProvider.AdminGroups);
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
            GetClaimsFromUserInfoEndpoint = true
        };
    }

    private static OidcProviderOptions CreateValidProviderOptions()
    {
        return new OidcProviderOptions
        {
            Enabled = true,
            ProviderId = "authelia",
            Name = "Authelia",
            Authority = "https://auth.example.com",
            ClientId = "jellyfin",
            ClientSecret = "client-secret",
            Scopes = ["openid", "profile", "email", "groups"],
            UsernameClaim = "preferred_username",
            RoleClaim = "groups",
            EmailClaim = "email",
            RequiredGroups = ["jellyfin"],
            AdminGroups = ["jellyfin-admins"],
            ProvisioningMode = OidcUserProvisioningMode.Disabled,
            GetClaimsFromUserInfoEndpoint = true
        };
    }

    private static (OidcConfigurationManager Manager, string TempDirectory) CreateManager(OidcOptions? initialOptions = null)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "jellyfin-oidc-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);

        if (initialOptions is not null)
        {
            using var stream = File.Create(Path.Combine(tempDirectory, "oidc.json"));
            JsonSerializer.Serialize(stream, initialOptions, JsonDefaults.Options);
        }

        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.Setup(paths => paths.ConfigurationDirectoryPath).Returns(tempDirectory);

        return (new OidcConfigurationManager(applicationPaths.Object), tempDirectory);
    }
}
