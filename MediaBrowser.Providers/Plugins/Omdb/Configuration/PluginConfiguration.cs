#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.Omdb.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool CastAndCrew { get; set; }
}
