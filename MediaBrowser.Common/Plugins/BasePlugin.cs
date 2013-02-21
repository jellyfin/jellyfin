using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Provides a common base class for all plugins
    /// </summary>
    /// <typeparam name="TConfigurationType">The type of the T configuration type.</typeparam>
    public abstract class BasePlugin<TConfigurationType> : IDisposable, IPlugin
        where TConfigurationType : BasePluginConfiguration
    {
        /// <summary>
        /// Gets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected IKernel Kernel { get; private set; }

        /// <summary>
        /// Gets or sets the plugin's current context
        /// </summary>
        /// <value>The context.</value>
        protected KernelContext Context { get { return Kernel.KernelContext; } }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public virtual string Description
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a core plugin.
        /// </summary>
        /// <value><c>true</c> if this instance is a core plugin; otherwise, <c>false</c>.</value>
        public virtual bool IsCorePlugin
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the type of configuration this plugin uses
        /// </summary>
        /// <value>The type of the configuration.</value>
        public Type ConfigurationType
        {
            get { return typeof(TConfigurationType); }
        }
        
        /// <summary>
        /// The _assembly name
        /// </summary>
        private AssemblyName _assemblyName;
        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>
        /// <value>The name of the assembly.</value>
        protected AssemblyName AssemblyName
        {
            get
            {
                return _assemblyName ?? (_assemblyName = GetType().Assembly.GetName());
            }
        }

        /// <summary>
        /// The _unique id
        /// </summary>
        private Guid? _uniqueId;

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public Guid UniqueId
        {
            get
            {

                if (!_uniqueId.HasValue)
                {
                    _uniqueId = Marshal.GetTypeLibGuidForAssembly(GetType().Assembly);
                }

                return _uniqueId.Value;
            }
        }

        /// <summary>
        /// Gets the plugin version
        /// </summary>
        /// <value>The version.</value>
        public Version Version
        {
            get
            {
                return AssemblyName.Version;
            }
        }

        /// <summary>
        /// Gets the name the assembly file
        /// </summary>
        /// <value>The name of the assembly file.</value>
        public string AssemblyFileName
        {
            get
            {
                return AssemblyName.Name + ".dll";
            }
        }

        /// <summary>
        /// Gets the last date modified of the configuration
        /// </summary>
        /// <value>The configuration date last modified.</value>
        public DateTime ConfigurationDateLastModified
        {
            get
            {
                // Ensure it's been lazy loaded
                var config = Configuration;

                return File.GetLastWriteTimeUtc(ConfigurationFilePath);
            }
        }

        /// <summary>
        /// Gets the last date modified of the plugin
        /// </summary>
        /// <value>The assembly date last modified.</value>
        public DateTime AssemblyDateLastModified
        {
            get
            {
                return File.GetLastWriteTimeUtc(AssemblyFilePath);
            }
        }

        /// <summary>
        /// Gets the path to the assembly file
        /// </summary>
        /// <value>The assembly file path.</value>
        public string AssemblyFilePath
        {
            get
            {
                return Path.Combine(Kernel.ApplicationPaths.PluginsPath, AssemblyFileName);
            }
        }

        /// <summary>
        /// The _configuration sync lock
        /// </summary>
        private object _configurationSyncLock = new object();
        /// <summary>
        /// The _configuration initialized
        /// </summary>
        private bool _configurationInitialized;
        /// <summary>
        /// The _configuration
        /// </summary>
        private TConfigurationType _configuration;
        /// <summary>
        /// Gets the plugin's configuration
        /// </summary>
        /// <value>The configuration.</value>
        public TConfigurationType Configuration
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _configuration, ref _configurationInitialized, ref _configurationSyncLock, () => XmlSerializer.GetXmlConfiguration(ConfigurationType, ConfigurationFilePath, Logger) as TConfigurationType);
                return _configuration;
            }
            protected set
            {
                _configuration = value;

                if (value == null)
                {
                    _configurationInitialized = false;
                }
            }
        }

        /// <summary>
        /// Gets the name of the configuration file. Subclasses should override
        /// </summary>
        /// <value>The name of the configuration file.</value>
        public virtual string ConfigurationFileName
        {
            get { return Path.ChangeExtension(AssemblyFileName, ".xml"); }
        }

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        /// <value>The configuration file path.</value>
        public string ConfigurationFilePath
        {
            get
            {
                return Path.Combine(Kernel.ApplicationPaths.PluginConfigurationsPath, ConfigurationFileName);
            }
        }

        /// <summary>
        /// The _data folder path
        /// </summary>
        private string _dataFolderPath;
        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed
        /// </summary>
        /// <value>The data folder path.</value>
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

        /// <summary>
        /// Returns true or false indicating if the plugin should be downloaded and run within the Ui.
        /// </summary>
        /// <value><c>true</c> if [download to UI]; otherwise, <c>false</c>.</value>
        public virtual bool DownloadToUi
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Starts the plugin.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">kernel</exception>
        public void Initialize(IKernel kernel, ILogger logger)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            
            Logger = logger;
            
            Kernel = kernel;

            if (kernel.KernelContext == KernelContext.Server)
            {
                InitializeOnServer(!File.Exists(ConfigurationFilePath));
            }
            else if (kernel.KernelContext == KernelContext.Ui)
            {
                InitializeInUi();
            }
        }

        /// <summary>
        /// Starts the plugin on the server
        /// </summary>
        /// <param name="isFirstRun">if set to <c>true</c> [is first run].</param>
        protected virtual void InitializeOnServer(bool isFirstRun)
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected void Dispose(bool dispose)
        {
            if (Kernel.KernelContext == KernelContext.Server)
            {
                DisposeOnServer(dispose);
            }
            else if (Kernel.KernelContext == KernelContext.Ui)
            {
                DisposeInUI(dispose);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void DisposeOnServer(bool dispose)
        {
            
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void DisposeInUI(bool dispose)
        {

        }

        /// <summary>
        /// The _save lock
        /// </summary>
        private readonly object _configurationSaveLock = new object();

        /// <summary>
        /// Saves the current configuration to the file system
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cannot call Plugin.SaveConfiguration from the UI.</exception>
        public virtual void SaveConfiguration()
        {
            if (Kernel.KernelContext != KernelContext.Server)
            {
                throw new InvalidOperationException("Cannot call Plugin.SaveConfiguration from the UI.");
            }

            Logger.Info("Saving configuration");

            lock (_configurationSaveLock)
            {
                XmlSerializer.SerializeToFile(Configuration, ConfigurationFilePath);
            }

            // Notify connected UI's
            Kernel.TcpManager.SendWebSocketMessage("PluginConfigurationUpdated-" + Name, Configuration);
        }

        /// <summary>
        /// Completely overwrites the current configuration with a new copy
        /// Returns true or false indicating success or failure
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">configuration</exception>
        public virtual void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }
            
            Configuration = (TConfigurationType)configuration;

            SaveConfiguration();
        }

        /// <summary>
        /// Gets the plugin info.
        /// </summary>
        /// <returns>PluginInfo.</returns>
        public PluginInfo GetPluginInfo()
        {
            var info = new PluginInfo
            {
                Name = Name,
                DownloadToUI = DownloadToUi,
                Version = Version.ToString(),
                AssemblyFileName = AssemblyFileName,
                ConfigurationDateLastModified = ConfigurationDateLastModified,
                Description = Description,
                IsCorePlugin = IsCorePlugin,
                UniqueId = UniqueId,
                EnableAutoUpdate = Configuration.EnableAutoUpdate,
                UpdateClass = Configuration.UpdateClass,
                ConfigurationFileName = ConfigurationFileName
            };

            var uiPlugin = this as IUIPlugin;

            if (uiPlugin != null)
            {
                info.MinimumRequiredUIVersion = uiPlugin.MinimumRequiredUIVersion.ToString();
            }

            return info;
        }

        /// <summary>
        /// Called when just before the plugin is uninstalled from the server.
        /// </summary>
        public virtual void OnUninstalling()
        {
            
        }

        /// <summary>
        /// Gets the plugin's configuration
        /// </summary>
        /// <value>The configuration.</value>
        BasePluginConfiguration IPlugin.Configuration
        {
            get { return Configuration; }
        }
    }
}
