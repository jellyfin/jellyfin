using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;
using MetaBrainz.MusicBrainz;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// Plugin instance.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="applicationHost">Instance of the <see cref="IApplicationHost"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IApplicationHost applicationHost)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;

        // TODO: Change this to "JellyfinMusicBrainzPlugin" once we take it out of the server repo.
        Query.DefaultUserAgent.Add(new ProductInfoHeaderValue(applicationHost.Name.Replace(' ', '-'), applicationHost.ApplicationVersionString));
        Query.DefaultUserAgent.Add(new ProductInfoHeaderValue($"({applicationHost.ApplicationUserAgentAddress})"));
        Query.DelayBetweenRequests = Instance.Configuration.RateLimit;
        Query.DefaultServer = Instance.Configuration.Server;
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

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
        };
    }
}
