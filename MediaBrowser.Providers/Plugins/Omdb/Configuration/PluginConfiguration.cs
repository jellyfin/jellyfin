#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool CastAndCrew { get; set; }
    }
}
