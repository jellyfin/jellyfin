using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool Enable { get; set; }

        public bool ReplaceAlbumName { get; set; }
    }
}
