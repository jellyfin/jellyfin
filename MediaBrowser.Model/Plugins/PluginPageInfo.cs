#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Plugins
{
    public class PluginPageInfo
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string EmbeddedResourcePath { get; set; }

        public bool EnableInMainMenu { get; set; }

        public string MenuSection { get; set; }

        public string MenuIcon { get; set; }
    }
}
