using System;
using System.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Provides a BasePlugin with generics, allowing for strongly typed configuration access.
    /// </summary>
    public abstract class BaseGenericPlugin<TConfigurationType> : BasePlugin
        where TConfigurationType : BasePluginConfiguration, new()
    {
        public new TConfigurationType Configuration
        {
            get
            {
                return base.Configuration as TConfigurationType;
            }
            set
            {
                base.Configuration = value;
            }
        }

        protected override Type ConfigurationType
        {
            get { return typeof(TConfigurationType); }
        }
    }

    /// <summary>
    /// Provides a common base class for all plugins
    /// </summary>
    public abstract class BasePlugin : IDisposable
    {
        private IKernel Kernel { get; set; }

        /// <summary>
        /// Gets or sets the plugin's current context
        /// </summary>
        protected KernelContext Context { get { return Kernel.KernelContext; } }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the type of configuration this plugin uses
        /// </summary>
        protected abstract Type ConfigurationType { get; }

        /// <summary>
        /// Gets the plugin version
        /// </summary>
        public Version Version
        {
            get
            {
                return GetType().Assembly.GetName().Version;
            }
        }

        /// <summary>
        /// Gets or sets the current plugin configuration
        /// </summary>
        public BasePluginConfiguration Configuration { get; protected set; }

        /// <summary>
        /// Gets the name of the configuration file. Subclasses should override
        /// </summary>
        public virtual string ConfigurationFileName { get { return Name + ".xml"; } }

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        public string ConfigurationFilePath
        {
            get
            {
                return Path.Combine(Kernel.ApplicationPaths.PluginConfigurationsPath, ConfigurationFileName);
            }
        }

        private string _DataFolderPath = null;
        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed
        /// </summary>
        public string DataFolderPath
        {
            get
            {
                if (_DataFolderPath == null)
                {
                    // Give the folder name the same name as the config file name
                    // We can always make this configurable if/when needed
                    _DataFolderPath = Path.Combine(Kernel.ApplicationPaths.PluginsPath, Path.GetFileNameWithoutExtension(ConfigurationFileName));

                    if (!Directory.Exists(_DataFolderPath))
                    {
                        Directory.CreateDirectory(_DataFolderPath);
                    }
                }

                return _DataFolderPath;
            }
        }

        public bool Enabled
        {
            get
            {
                return Configuration.Enabled;
            }
        }

        /// <summary>
        /// Returns true or false indicating if the plugin should be downloaded and run within the UI.
        /// </summary>
        public virtual bool DownloadToUI
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Starts the plugin.
        /// </summary>
        public void Initialize(IKernel kernel)
        {
            Kernel = kernel;

            ReloadConfiguration();

            if (Enabled)
            {
                InitializeInternal();
            }
        }

        /// <summary>
        /// Starts the plugin.
        /// </summary>
        protected virtual void InitializeInternal()
        {
        }

        /// <summary>
        /// Disposes the plugins. Undos all actions performed during Init.
        /// </summary>
        public virtual void Dispose()
        {
        }

        public void ReloadConfiguration()
        {
            if (!File.Exists(ConfigurationFilePath))
            {
                Configuration = Activator.CreateInstance(ConfigurationType) as BasePluginConfiguration;
            }
            else
            {
                Configuration = XmlSerializer.DeserializeFromFile(ConfigurationType, ConfigurationFilePath) as BasePluginConfiguration;
                Configuration.DateLastModified = File.GetLastWriteTime(ConfigurationFilePath);
            }
        }
    }
}
