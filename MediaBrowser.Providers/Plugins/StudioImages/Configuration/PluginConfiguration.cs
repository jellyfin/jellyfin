#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.StudioImages.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        private string _repository = Plugin.DefaultServer;

        public string RepositoryUrl
        {
            get
            {
                if (string.IsNullOrEmpty(_repository))
                {
                    _repository = Plugin.DefaultServer;
                }

                return _repository;
            }

            set
            {
                _repository = string.IsNullOrEmpty(value)
                    ? Plugin.DefaultServer
                    : value.TrimEnd('/');
            }
        }
    }
}
