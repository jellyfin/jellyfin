using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Threading;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;
using MetaBrainz.MusicBrainz;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// Plugin instance.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IDisposable
{
    private readonly ILogger<Plugin> _logger;
    private readonly Lock _queryLock = new();
    private Query _musicBrainzQuery;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="applicationHost">Instance of the <see cref="IApplicationHost"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{Plugin}"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IApplicationHost applicationHost, ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _logger = logger;

        // TODO: Change this to "JellyfinMusicBrainzPlugin" once we take it out of the server repo.
        Query.DefaultUserAgent.Add(new ProductInfoHeaderValue(applicationHost.Name.Replace(' ', '-'), applicationHost.ApplicationVersionString));
        Query.DefaultUserAgent.Add(new ProductInfoHeaderValue($"({applicationHost.ApplicationUserAgentAddress})"));

        ApplyServerConfig(Configuration);
        Query.DelayBetweenRequests = Configuration.RateLimit;
        _musicBrainzQuery = new Query();

        ConfigurationChanged += OnConfigurationChanged;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => new Guid("8c95c4d2-e50c-4fb0-a4f3-6c06ff0f9a1a");

    /// <inheritdoc />
    public override string Name => "MusicBrainz";

    /// <inheritdoc />
    public override string Description => "Get artist and album metadata from any MusicBrainz server.";

    /// <inheritdoc />
    // TODO remove when plugin removed from server.
    public override string ConfigurationFileName => "Jellyfin.Plugin.MusicBrainz.xml";

    /// <summary>
    /// Gets the current MusicBrainz query client.
    /// </summary>
    /// <remarks>
    /// Always read this property anew before each request — the underlying instance is
    /// replaced when the server URL changes. Old instances are intentionally left alive
    /// so in-flight requests can finish; their unmanaged resources leak until GC.
    /// </remarks>
    public Query MusicBrainzQuery
    {
        get
        {
            lock (_queryLock)
            {
                return _musicBrainzQuery;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and managed resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            ConfigurationChanged -= OnConfigurationChanged;
            lock (_queryLock)
            {
                _musicBrainzQuery.Dispose();
            }
        }

        _disposed = true;
    }

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP003:Dispose previous before re-assigning", Justification = "The previous Query may still be in use by in-flight async requests; disposing it would cause ObjectDisposedException. The orphan is intentionally left for GC.")]
    private void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
    {
        var configuration = (PluginConfiguration)e;
        ApplyServerConfig(configuration);
        Query.DelayBetweenRequests = configuration.RateLimit;

        lock (_queryLock)
        {
            _musicBrainzQuery = new Query();
        }
    }

    private void ApplyServerConfig(PluginConfiguration configuration)
    {
        if (Uri.TryCreate(configuration.Server, UriKind.Absolute, out var server))
        {
            Query.DefaultServer = server.DnsSafeHost;
            Query.DefaultPort = server.Port;
            Query.DefaultUrlScheme = server.Scheme;
        }
        else
        {
            _logger.LogWarning("Invalid MusicBrainz server specified, falling back to official server");
            var defaultServer = new Uri(PluginConfiguration.DefaultServer);
            Query.DefaultServer = defaultServer.Host;
            Query.DefaultPort = defaultServer.Port;
            Query.DefaultUrlScheme = defaultServer.Scheme;
        }
    }
}
