using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.ScheduledTasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Plugins.Trailers.Configuration;
using MediaBrowser.Plugins.Trailers.ScheduledTasks;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace MediaBrowser.Plugins.Trailers
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    [Export(typeof(IPlugin))]
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Trailers"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Movie trailers for your collection.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        public Plugin()
            : base()
        {
            Instance = this;
        }

        /// <summary>
        /// The _download path
        /// </summary>
        private string _downloadPath;
        /// <summary>
        /// Gets the path to the trailer download directory
        /// </summary>
        /// <value>The download path.</value>
        public string DownloadPath
        {
            get
            {
                if (_downloadPath == null)
                {
                    // Use 
                    _downloadPath = Configuration.DownloadPath;

                    if (string.IsNullOrWhiteSpace(_downloadPath))
                    {
                        _downloadPath = Path.Combine(Controller.Kernel.Instance.ApplicationPaths.DataPath, Name);
                    }

                    if (!Directory.Exists(_downloadPath))
                    {
                        Directory.CreateDirectory(_downloadPath);
                    }
                }
                return _downloadPath;
            }
        }

        /// <summary>
        /// Starts the plugin on the server
        /// </summary>
        /// <param name="isFirstRun">if set to <c>true</c> [is first run].</param>
        protected override void InitializeOnServer(bool isFirstRun)
        {
            base.InitializeOnServer(isFirstRun);

            if (isFirstRun)
            {
                Kernel.TaskManager.QueueScheduledTask<CurrentTrailerDownloadTask>();
            }
        }

        /// <summary>
        /// Completely overwrites the current configuration with a new copy
        /// Returns true or false indicating success or failure
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var config = (PluginConfiguration) configuration;

            var pathChanged = !string.Equals(Configuration.DownloadPath, config.DownloadPath, StringComparison.OrdinalIgnoreCase);

            base.UpdateConfiguration(configuration);

            if (pathChanged)
            {
                _downloadPath = null;
                Kernel.TaskManager.QueueScheduledTask<RefreshMediaLibraryTask>();
            }
        }
    }
}
