using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Authentication;

namespace Jellyfin.Server.Implementations.Authentication;

/// <summary>
/// File-backed OpenID Connect configuration manager.
/// </summary>
public class OidcConfigurationManager : IOidcConfigurationManager
{
    private const string ConfigurationFileName = "oidc.json";
    private readonly string _configurationPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcConfigurationManager"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    public OidcConfigurationManager(IApplicationPaths applicationPaths)
    {
        _configurationPath = Path.Combine(applicationPaths.ConfigurationDirectoryPath, ConfigurationFileName);
    }

    /// <inheritdoc />
    public OidcOptions GetOptions()
    {
        if (!File.Exists(_configurationPath))
        {
            return new OidcOptions();
        }

        using var stream = File.OpenRead(_configurationPath);
        var options = JsonSerializer.Deserialize<OidcOptions>(stream, JsonDefaults.Options);
        return Normalize(options ?? new OidcOptions());
    }

    /// <inheritdoc />
    public IReadOnlyList<OidcProviderInfo> GetProviderInfos()
    {
        return GetOptions().Providers
            .Where(provider => provider.Enabled)
            .Select(provider => new OidcProviderInfo
            {
                ProviderId = provider.ProviderId,
                Name = provider.Name,
                Authority = provider.Authority,
                DeviceAuthorizationEnabled = provider.EnableDeviceAuthorization
            })
            .ToList();
    }

    /// <inheritdoc />
    public OidcConfigurationDto GetConfiguration()
    {
        return new OidcConfigurationDto
        {
            Providers = GetOptions().Providers
                .Select(ToConfigurationDto)
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task UpdateConfigurationAsync(OidcConfigurationUpdateDto configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var currentByProviderId = GetOptions().Providers
            .Where(provider => !string.IsNullOrWhiteSpace(provider.ProviderId))
            .GroupBy(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase);

        var updated = new OidcOptions
        {
            Providers = (configuration.Providers ?? new List<OidcProviderConfigurationUpdateDto>())
                .Select(provider => ToProviderOptions(provider, currentByProviderId))
                .ToList()
        };

        Validate(updated);
        Directory.CreateDirectory(Path.GetDirectoryName(_configurationPath)!);

        await using var stream = File.Create(_configurationPath);
        await JsonSerializer.SerializeAsync(stream, updated, JsonDefaults.Options, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public OidcProviderOptions? GetEnabledProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return null;
        }

        return GetOptions().Providers
            .FirstOrDefault(provider => provider.Enabled && string.Equals(provider.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
    }

    private static OidcProviderConfigurationDto ToConfigurationDto(OidcProviderOptions provider)
    {
        return new OidcProviderConfigurationDto
        {
            Enabled = provider.Enabled,
            ProviderId = provider.ProviderId,
            Name = provider.Name,
            Authority = provider.Authority,
            ClientId = provider.ClientId,
            HasClientSecret = !string.IsNullOrWhiteSpace(provider.ClientSecret),
            AllowInsecureAuthority = provider.AllowInsecureAuthority,
            Scopes = provider.Scopes,
            UsernameClaim = provider.UsernameClaim,
            RoleClaim = provider.RoleClaim,
            EmailClaim = provider.EmailClaim,
            RequiredGroups = provider.RequiredGroups,
            AdminGroups = provider.AdminGroups,
            ProvisioningMode = provider.ProvisioningMode,
            SyncAdminRole = provider.SyncAdminRole,
            GetClaimsFromUserInfoEndpoint = provider.GetClaimsFromUserInfoEndpoint,
            EnableDeviceAuthorization = provider.EnableDeviceAuthorization,
            EnableRpInitiatedLogout = provider.EnableRpInitiatedLogout
        };
    }

    private static OidcProviderOptions ToProviderOptions(
        OidcProviderConfigurationUpdateDto provider,
        IReadOnlyDictionary<string, OidcProviderOptions> currentByProviderId)
    {
        var providerId = NormalizeString(provider.ProviderId);
        currentByProviderId.TryGetValue(providerId, out var currentProvider);

        return NormalizeProvider(new OidcProviderOptions
        {
            Enabled = provider.Enabled,
            ProviderId = providerId,
            Name = NormalizeString(provider.Name),
            Authority = NormalizeString(provider.Authority),
            ClientId = NormalizeString(provider.ClientId),
            ClientSecret = string.IsNullOrWhiteSpace(provider.ClientSecret) ? currentProvider?.ClientSecret ?? string.Empty : provider.ClientSecret.Trim(),
            AllowInsecureAuthority = provider.AllowInsecureAuthority,
            Scopes = provider.Scopes,
            UsernameClaim = NormalizeString(provider.UsernameClaim),
            RoleClaim = NormalizeString(provider.RoleClaim),
            EmailClaim = NormalizeString(provider.EmailClaim),
            RequiredGroups = provider.RequiredGroups,
            AdminGroups = provider.AdminGroups,
            ProvisioningMode = provider.ProvisioningMode,
            SyncAdminRole = provider.SyncAdminRole,
            GetClaimsFromUserInfoEndpoint = provider.GetClaimsFromUserInfoEndpoint,
            EnableDeviceAuthorization = provider.EnableDeviceAuthorization,
            EnableRpInitiatedLogout = provider.EnableRpInitiatedLogout
        });
    }

    private static OidcOptions Normalize(OidcOptions options)
    {
        return new OidcOptions
        {
            Providers = (options.Providers ?? new List<OidcProviderOptions>()).Select(NormalizeProvider).ToList()
        };
    }

    private static OidcProviderOptions NormalizeProvider(OidcProviderOptions provider)
    {
        var scopes = NormalizeStrings(provider.Scopes);
        if (scopes.Count == 0)
        {
            scopes = new List<string> { "openid", "profile", "email", "groups" };
        }

        return new OidcProviderOptions
        {
            Enabled = provider.Enabled,
            ProviderId = NormalizeString(provider.ProviderId),
            Name = string.IsNullOrWhiteSpace(provider.Name) ? NormalizeString(provider.ProviderId) : NormalizeString(provider.Name),
            Authority = NormalizeString(provider.Authority).TrimEnd('/'),
            ClientId = NormalizeString(provider.ClientId),
            ClientSecret = NormalizeString(provider.ClientSecret),
            AllowInsecureAuthority = provider.AllowInsecureAuthority,
            Scopes = scopes,
            UsernameClaim = string.IsNullOrWhiteSpace(provider.UsernameClaim) ? "preferred_username" : provider.UsernameClaim.Trim(),
            RoleClaim = string.IsNullOrWhiteSpace(provider.RoleClaim) ? "groups" : provider.RoleClaim.Trim(),
            EmailClaim = string.IsNullOrWhiteSpace(provider.EmailClaim) ? "email" : provider.EmailClaim.Trim(),
            RequiredGroups = NormalizeStrings(provider.RequiredGroups),
            AdminGroups = NormalizeStrings(provider.AdminGroups),
            ProvisioningMode = provider.ProvisioningMode,
            SyncAdminRole = provider.SyncAdminRole,
            GetClaimsFromUserInfoEndpoint = provider.GetClaimsFromUserInfoEndpoint,
            EnableDeviceAuthorization = provider.EnableDeviceAuthorization,
            EnableRpInitiatedLogout = provider.EnableRpInitiatedLogout
        };
    }

    private static string NormalizeString(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static List<string> NormalizeStrings(IEnumerable<string>? values)
    {
        return (values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Validate(OidcOptions options)
    {
        var seenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in options.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.ProviderId))
            {
                if (provider.Enabled)
                {
                    throw new ArgumentException("Enabled OIDC providers require a provider id.");
                }

                continue;
            }

            if (!seenProviderIds.Add(provider.ProviderId))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider id '{0}' is configured more than once.", provider.ProviderId));
            }

            if (!IsValidProviderId(provider.ProviderId))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider id '{0}' is invalid.", provider.ProviderId));
            }

            if (!provider.Enabled)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(provider.Authority)
                || !Uri.TryCreate(provider.Authority, UriKind.Absolute, out var authority))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider '{0}' has an invalid authority.", provider.ProviderId));
            }

            if (!string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(authority.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider '{0}' must use an HTTP or HTTPS authority.", provider.ProviderId));
            }

            if (!provider.AllowInsecureAuthority
                && !string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider '{0}' must use an HTTPS authority unless insecure authority is explicitly allowed.", provider.ProviderId));
            }

            if (string.IsNullOrWhiteSpace(provider.ClientId))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider '{0}' requires a client id.", provider.ProviderId));
            }

            if (string.IsNullOrWhiteSpace(provider.ClientSecret))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider '{0}' requires a client secret.", provider.ProviderId));
            }

            if (!provider.Scopes.Contains("openid", StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider '{0}' must request the openid scope.", provider.ProviderId));
            }
        }
    }

    private static bool IsValidProviderId(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId) || providerId.Length > 64)
        {
            return false;
        }

        foreach (var c in providerId)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')
            {
                return false;
            }
        }

        return true;
    }
}
