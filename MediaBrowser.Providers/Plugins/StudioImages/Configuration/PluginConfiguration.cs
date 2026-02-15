using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.StudioImages.Configuration
{
    /// <summary>
    /// Plugin configuration class for the studio image provider.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        private string _repository = Plugin.DefaultServer;

        /// <summary>
        /// Gets or sets the studio image repository URL.
        /// </summary>
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
