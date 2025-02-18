#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.AudioDb.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool ReplaceAlbumName { get; set; }
}
