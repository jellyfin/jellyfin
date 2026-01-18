using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.ListenBrainz.Configuration;

namespace MediaBrowser.Providers.Plugins.ListenBrainz;

/// <summary>
/// ListenBrainz plugin instance.
/// </summary>
public class ListenBrainzPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public ListenBrainzPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static ListenBrainzPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => new("a5b2e8c1-9d4f-4a3b-8c7e-6f1a2b3c4d5e");

    /// <inheritdoc />
    public override string Name => "ListenBrainz";

    /// <inheritdoc />
    public override string Description => "Get similar artist recommendations from ListenBrainz Labs.";

    /// <inheritdoc />
    public override string ConfigurationFileName => "Jellyfin.Plugin.ListenBrainz.xml";

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
