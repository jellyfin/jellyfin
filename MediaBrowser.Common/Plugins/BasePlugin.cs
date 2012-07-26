using System;
using System.IO;
using MediaBrowser.Common.Json;
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

        public override void ReloadConfiguration()
        {
            if (!File.Exists(ConfigurationPath))
            {
                Configuration = new TConfigurationType();
            }
            else
            {
                Configuration = JsonSerializer.DeserializeFromFile<TConfigurationType>(ConfigurationPath);
                Configuration.DateLastModified = File.GetLastWriteTime(ConfigurationPath);
            }
        }
    }

    /// <summary>
    /// Provides a common base class for all plugins
    /// </summary>
    public abstract class BasePlugin
    {
        public abstract string Name { get; }

        public string Path { get; set; }

        public Version Version { get; set; }

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

        public abstract void ReloadConfiguration();

        public virtual void InitInServer()
        {
        }

        public virtual void InitInUI()
        {
        }

    }
}
