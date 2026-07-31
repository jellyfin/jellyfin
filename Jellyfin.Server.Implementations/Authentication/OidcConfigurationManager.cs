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
    private const UnixFileMode SecretFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private readonly string _configurationPath;
    private readonly OidcOptions _activeOptions;
    private readonly object _configurationLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcConfigurationManager"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    public OidcConfigurationManager(IApplicationPaths applicationPaths)
    {
        _configurationPath = Path.Combine(applicationPaths.ConfigurationDirectoryPath, ConfigurationFileName);
        _activeOptions = CloneOptions(ReadOptions());
    }

    /// <inheritdoc />
    public OidcOptions GetOptions()
    {
        lock (_configurationLock)
        {
            return ReadOptions();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<OidcProviderInfo> GetProviderInfos()
    {
        return _activeOptions.Providers
            .Where(provider => provider.Enabled)
            .Select(ToProviderInfo)
            .ToList();
    }

    /// <inheritdoc />
    public OidcConfigurationDto GetConfiguration()
    {
        var options = GetOptions();
        return new OidcConfigurationDto
        {
            Providers = options.Providers
                .Select(ToConfigurationDto)
                .ToList(),
            RequiresRestart = !OptionsEqual(options, _activeOptions)
        };
    }

    /// <inheritdoc />
    public Task UpdateConfigurationAsync(OidcConfigurationUpdateDto configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_configurationLock)
        {
            var currentByProviderId = ReadOptions().Providers
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
            WriteOptions(updated);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public OidcProviderOptions? GetEnabledProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return null;
        }

        var provider = _activeOptions.Providers
            .FirstOrDefault(provider => provider.Enabled && string.Equals(provider.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));

        return provider is null ? null : CloneProvider(provider);
    }

    private OidcOptions ReadOptions()
    {
        if (!File.Exists(_configurationPath))
        {
            return new OidcOptions();
        }

        using var stream = File.OpenRead(_configurationPath);
        var options = JsonSerializer.Deserialize<OidcOptions>(stream, JsonDefaults.Options);
        return Normalize(options ?? new OidcOptions());
    }

    private void WriteOptions(OidcOptions options)
    {
        var directory = Path.GetDirectoryName(_configurationPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("The OIDC configuration path must include a directory.");
        }

        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, ConfigurationFileName + "." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp");
        try
        {
            using (var stream = CreateSecretTempFile(tempPath))
            {
                JsonSerializer.Serialize(stream, options, JsonDefaults.Options);
                stream.Flush(true);
            }

            SetSecretFileMode(tempPath);
            if (File.Exists(_configurationPath))
            {
                File.Replace(tempPath, _configurationPath, null);
            }
            else
            {
                File.Move(tempPath, _configurationPath);
            }

            SetSecretFileMode(_configurationPath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static void SetSecretFileMode(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, SecretFileMode);
        }
    }

    private static FileStream CreateSecretTempFile(string path)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 4096,
            Options = FileOptions.WriteThrough
        };

        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = SecretFileMode;
        }

        return new FileStream(path, options);
    }

    private static OidcProviderConfigurationDto ToConfigurationDto(OidcProviderOptions provider)
    {
        return CopyProviderConfiguration(provider, new OidcProviderConfigurationDto
        {
            HasClientSecret = !string.IsNullOrWhiteSpace(provider.ClientSecret)
        });
    }

    private static OidcProviderInfo ToProviderInfo(OidcProviderOptions provider)
    {
        return new OidcProviderInfo
        {
            ProviderId = provider.ProviderId,
            Name = provider.Name,
            Authority = provider.Authority
        };
    }

    private static OidcProviderOptions ToProviderOptions(
        OidcProviderConfigurationUpdateDto provider,
        IReadOnlyDictionary<string, OidcProviderOptions> currentByProviderId)
    {
        var providerId = NormalizeString(provider.ProviderId);
        currentByProviderId.TryGetValue(providerId, out var currentProvider);

        var options = CopyProviderConfiguration(provider, new OidcProviderOptions
        {
            ClientSecret = GetClientSecret(provider.ClientSecret, currentProvider)
        });
        options.ProviderId = providerId;
        return NormalizeProvider(options);
    }

    private static OidcOptions Normalize(OidcOptions options)
    {
        return new OidcOptions
        {
            Providers = (options.Providers ?? new List<OidcProviderOptions>()).Select(NormalizeProvider).ToList()
        };
    }

    private static OidcOptions CloneOptions(OidcOptions options)
    {
        return new OidcOptions
        {
            Providers = options.Providers.Select(CloneProvider).ToList()
        };
    }

    private static OidcProviderOptions CloneProvider(OidcProviderOptions provider)
    {
        return CopyProviderConfiguration(provider, new OidcProviderOptions
        {
            ClientSecret = provider.ClientSecret
        });
    }

    private static T CopyProviderConfiguration<T>(OidcProviderConfigurationBase source, T target)
        where T : OidcProviderConfigurationBase
    {
        target.Enabled = source.Enabled;
        target.ProviderId = source.ProviderId;
        target.Name = source.Name;
        target.Authority = source.Authority;
        target.ClientId = source.ClientId;
        target.AllowInsecureAuthority = source.AllowInsecureAuthority;
        target.Scopes = source.Scopes?.ToList() ?? new List<string>();
        target.UsernameClaim = source.UsernameClaim;
        target.RoleClaim = source.RoleClaim;
        target.EmailClaim = source.EmailClaim;
        target.RequiredGroups = source.RequiredGroups?.ToList() ?? new List<string>();
        target.AdminGroups = source.AdminGroups?.ToList() ?? new List<string>();
        target.ProvisioningMode = source.ProvisioningMode;
        target.SyncAdminRole = source.SyncAdminRole;
        target.GetClaimsFromUserInfoEndpoint = source.GetClaimsFromUserInfoEndpoint;
        return target;
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
            GetClaimsFromUserInfoEndpoint = provider.GetClaimsFromUserInfoEndpoint
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

    private static string GetClientSecret(string? clientSecret, OidcProviderOptions? currentProvider)
    {
        return string.IsNullOrWhiteSpace(clientSecret)
            ? currentProvider?.ClientSecret ?? string.Empty
            : clientSecret.Trim();
    }

    private static void Validate(OidcOptions options)
    {
        var seenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in options.Providers)
        {
            if (!ValidateProviderIdPresence(provider))
            {
                continue;
            }

            ValidateProviderId(provider, seenProviderIds);
            if (provider.Enabled)
            {
                ValidateEnabledProvider(provider);
            }
        }
    }

    private static bool ValidateProviderIdPresence(OidcProviderOptions provider)
    {
        if (!string.IsNullOrWhiteSpace(provider.ProviderId))
        {
            return true;
        }

        if (provider.Enabled)
        {
            throw new ArgumentException("Enabled OIDC providers require a provider id.");
        }

        return false;
    }

    private static void ValidateProviderId(OidcProviderOptions provider, HashSet<string> seenProviderIds)
    {
        if (!seenProviderIds.Add(provider.ProviderId))
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider id '{0}' is configured more than once.", provider.ProviderId));
        }

        if (!IsValidProviderId(provider.ProviderId))
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "OIDC provider id '{0}' is invalid.", provider.ProviderId));
        }
    }

    private static void ValidateEnabledProvider(OidcProviderOptions provider)
    {
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

    private static bool IsValidProviderId(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId) || providerId.Length > 64)
        {
            return false;
        }

        return providerId.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    }

    private static bool OptionsEqual(OidcOptions left, OidcOptions right)
    {
        return JsonSerializer.Serialize(left, JsonDefaults.Options) == JsonSerializer.Serialize(right, JsonDefaults.Options);
    }
}
