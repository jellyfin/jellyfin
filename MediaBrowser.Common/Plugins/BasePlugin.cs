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
        /// <summary>
        /// Gets or sets the plugin's current context
        /// </summary>
        public KernelContext Context { get; set; }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the type of configuration this plugin uses
        /// </summary>
        protected abstract Type ConfigurationType { get; }

        /// <summary>
        /// Gets or sets the path to the plugin's folder
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the plugin version
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Gets or sets the current plugin configuration
        /// </summary>
        public BasePluginConfiguration Configuration { get; protected set; }

        protected string ConfigurationPath
        {
            get
            {
                return System.IO.Path.Combine(Path, "config.js");
            }
        }

        public bool Enabled
        {
            get
            {
                return Configuration.Enabled;
            }
        }

        public DateTime ConfigurationDateLastModified
        {
            get
            {
                return Configuration.DateLastModified;
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
        public virtual void Init()
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
            if (!File.Exists(ConfigurationPath))
            {
                Configuration = Activator.CreateInstance(ConfigurationType) as BasePluginConfiguration;
            }
            else
            {
                Configuration = JsonSerializer.DeserializeFromFile(ConfigurationType, ConfigurationPath) as BasePluginConfiguration;
                Configuration.DateLastModified = File.GetLastWriteTime(ConfigurationPath);
            }
        }
    }
}
