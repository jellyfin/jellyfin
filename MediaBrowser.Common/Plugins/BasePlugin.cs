using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Plugins;
using System;
using System.IO;
using System.Reflection;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Provides a common base class for all plugins
    /// </summary>
    public abstract class BasePlugin : IDisposable
    {
        protected IKernel Kernel { get; private set; }

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
        public virtual Type ConfigurationType
        {
            get { return typeof (BasePluginConfiguration); }
        }

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
        /// Gets the name the assembly file
        /// </summary>
        public string AssemblyFileName
        {
            get
            {
                return GetType().Assembly.GetName().Name + ".dll";
            }
        }

        private DateTime? _configurationDateLastModified;
        public DateTime ConfigurationDateLastModified
        {
            get
            {
                if (_configurationDateLastModified == null)
                {
                    if (File.Exists(ConfigurationFilePath))
                    {
                        _configurationDateLastModified = File.GetLastWriteTimeUtc(ConfigurationFilePath);
                    }
                }

                return _configurationDateLastModified ?? DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the path to the assembly file
        /// </summary>
        public string AssemblyFilePath
        {
            get
            {
                return Path.Combine(Kernel.ApplicationPaths.PluginsPath, AssemblyFileName);
            }
        }

        /// <summary>
        /// Gets or sets the current plugin configuration
        /// </summary>
        public BasePluginConfiguration Configuration { get; protected set; }

        /// <summary>
        /// Gets the name of the configuration file. Subclasses should override
        /// </summary>
        public virtual string ConfigurationFileName
        {
            get
            {
                return Name.Replace(" ", string.Empty) + ".xml";
            }
        }

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

        private string _dataFolderPath;
        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed
        /// </summary>
        public string DataFolderPath
        {
            get
            {
                if (_dataFolderPath == null)
                {
                    // Give the folder name the same name as the config file name
                    // We can always make this configurable if/when needed
                    _dataFolderPath = Path.Combine(Kernel.ApplicationPaths.PluginsPath, Path.GetFileNameWithoutExtension(ConfigurationFileName));

                    if (!Directory.Exists(_dataFolderPath))
                    {
                        Directory.CreateDirectory(_dataFolderPath);
                    }
                }

                return _dataFolderPath;
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
        /// Returns true or false indicating if the plugin should be downloaded and run within the Ui.
        /// </summary>
        public virtual bool DownloadToUi
        {
            get
            {
                return false;
            }
        }

        public void Initialize(IKernel kernel)
        {
            Initialize(kernel, true);
        }

        /// <summary>
        /// Starts the plugin.
        /// </summary>
        public void Initialize(IKernel kernel, bool loadFeatures)
        {
            Kernel = kernel;

            if (loadFeatures)
            {
                ReloadConfiguration();

                if (Enabled)
                {
                    if (kernel.KernelContext == KernelContext.Server)
                    {
                        InitializeOnServer();
                    }
                    else if (kernel.KernelContext == KernelContext.Ui)
                    {
                        InitializeInUi();
                    }
                }
            }
        }

        /// <summary>
        /// Starts the plugin on the server
        /// </summary>
        protected virtual void InitializeOnServer()
        {
        }

        /// <summary>
        /// Starts the plugin in the Ui
        /// </summary>
        protected virtual void InitializeInUi()
        {
        }

        /// <summary>
        /// Disposes the plugins. Undos all actions performed during Init.
        /// </summary>
        public void Dispose()
        {
            Logger.LogInfo("Disposing {0} Plugin", Name);

            if (Context == KernelContext.Server)
            {
                DisposeOnServer();
            }
            else if (Context == KernelContext.Ui)
            {
                InitializeInUi();
            }
        }

        /// <summary>
        /// Disposes the plugin on the server
        /// </summary>
        protected virtual void DisposeOnServer()
        {
        }

        /// <summary>
        /// Disposes the plugin in the Ui
        /// </summary>
        protected virtual void DisposeInUi()
        {
        }

        public void ReloadConfiguration()
        {
            if (!File.Exists(ConfigurationFilePath))
            {
                Configuration = Activator.CreateInstance(ConfigurationType) as BasePluginConfiguration;
                XmlSerializer.SerializeToFile(Configuration, ConfigurationFilePath);
            }
            else
            {
                Configuration = XmlSerializer.DeserializeFromFile(ConfigurationType, ConfigurationFilePath) as BasePluginConfiguration;
            }

            // Reset this so it will be loaded again next time it's accessed
            _configurationDateLastModified = null;
        }
    }
}
