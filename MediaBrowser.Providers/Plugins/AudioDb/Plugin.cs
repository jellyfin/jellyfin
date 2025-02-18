#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.AudioDb.Configuration;

namespace MediaBrowser.Providers.Plugins.AudioDb;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin Instance { get; private set; }

    public override Guid Id => new Guid("a629c0da-fac5-4c7e-931a-7174223f14c8");

    public override string Name => "AudioDB";

    public override string Description => "Get artist and album metadata or images from AudioDB.";

    // TODO remove when plugin removed from server.
    public override string ConfigurationFileName => "Jellyfin.Plugin.AudioDb.xml";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
        };
    }
}
