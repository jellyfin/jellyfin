#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.StudioImages
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        private string _repository = Plugin.DefaultServer;

        public string RepositoryUrl
        {
            get
            {
                return _repository;
            }

            set
            {
                _repository = value.TrimEnd('/');
            }
        }
    }
}
