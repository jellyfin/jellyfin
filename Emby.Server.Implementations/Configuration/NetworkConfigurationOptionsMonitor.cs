using System;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Emby.Server.Implementations.Configuration;

/// <summary>
/// Adapter that wraps <see cref="IConfiguration"/> to provide <see cref="IOptionsMonitor{NetworkConfiguration}"/>
/// before the DI container is built.
/// </summary>
internal sealed class NetworkConfigurationOptionsMonitor : IOptionsMonitor<NetworkConfiguration>
{
    private const string SectionKey = "NetworkConfiguration";

    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConfigurationOptionsMonitor"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public NetworkConfigurationOptionsMonitor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public NetworkConfiguration CurrentValue
        => _configuration.GetSection(SectionKey).Get<NetworkConfiguration>() ?? new NetworkConfiguration();

    /// <inheritdoc/>
    public NetworkConfiguration Get(string? name) => CurrentValue;

    /// <inheritdoc/>
    public IDisposable? OnChange(Action<NetworkConfiguration, string?> listener)
        => ChangeToken.OnChange(_configuration.GetReloadToken, () => listener(CurrentValue, null));
}
