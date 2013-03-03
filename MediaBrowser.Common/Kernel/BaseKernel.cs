using MediaBrowser.Common.Events;
using MediaBrowser.Common.Security;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Represents a shared base kernel for both the Ui and server apps
    /// </summary>
    /// <typeparam name="TConfigurationType">The type of the T configuration type.</typeparam>
    /// <typeparam name="TApplicationPathsType">The type of the T application paths type.</typeparam>
    public abstract class BaseKernel<TConfigurationType, TApplicationPathsType> : IDisposable, IKernel
        where TConfigurationType : BaseApplicationConfiguration, new()
        where TApplicationPathsType : IApplicationPaths
    {
        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        public event EventHandler HasPendingRestartChanged;

        #region ConfigurationUpdated Event
        /// <summary>
        /// Occurs when [configuration updated].
        /// </summary>
        public event EventHandler<EventArgs> ConfigurationUpdated;

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        internal void OnConfigurationUpdated()
        {
            EventHelper.QueueEventIfNotNull(ConfigurationUpdated, this, EventArgs.Empty, Logger);
        }
        #endregion

        #region ApplicationUpdated Event
        /// <summary>
        /// Occurs when [application updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<Version>> ApplicationUpdated;
        /// <summary>
        /// Called when [application updated].
        /// </summary>
        /// <param name="newVersion">The new version.</param>
        public void OnApplicationUpdated(Version newVersion)
        {
            EventHelper.QueueEventIfNotNull(ApplicationUpdated, this, new GenericEventArgs<Version> { Argument = newVersion }, Logger);

            NotifyPendingRestart();
        }
        #endregion

        /// <summary>
        /// The _configuration loaded
        /// </summary>
        private bool _configurationLoaded;
        /// <summary>
        /// The _configuration sync lock
        /// </summary>
        private object _configurationSyncLock = new object();
        /// <summary>
        /// The _configuration
        /// </summary>
        private TConfigurationType _configuration;
        /// <summary>
        /// Gets the system configuration
        /// </summary>
        /// <value>The configuration.</value>
        public TConfigurationType Configuration
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _configuration, ref _configurationLoaded, ref _configurationSyncLock, () => GetXmlConfiguration<TConfigurationType>(ApplicationPaths.SystemConfigurationFilePath));
                return _configuration;
            }
            protected set
            {
                _configuration = value;

                if (value == null)
                {
                    _configurationLoaded = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has changes that require the entire application to restart.
        /// </summary>
        /// <value><c>true</c> if this instance has pending application restart; otherwise, <c>false</c>.</value>
        public bool HasPendingRestart { get; private set; }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public TApplicationPathsType ApplicationPaths { get; private set; }

        /// <summary>
        /// Gets or sets the TCP manager.
        /// </summary>
        /// <value>The TCP manager.</value>
        private IServerManager ServerManager { get; set; }

        /// <summary>
        /// Gets the plug-in security manager.
        /// </summary>
        /// <value>The plug-in security manager.</value>
        public ISecurityManager SecurityManager { get; set; }

        /// <summary>
        /// Gets the UDP server port number.
        /// This can't be configurable because then the user would have to configure their client to discover the server.
        /// </summary>
        /// <value>The UDP server port number.</value>
        public abstract int UdpServerPortNumber { get; }

        /// <summary>
        /// Gets the name of the web application that can be used for url building.
        /// All api urls will be of the form {protocol}://{host}:{port}/{appname}/...
        /// </summary>
        /// <value>The name of the web application.</value>
        public string WebApplicationName
        {
            get { return "mediabrowser"; }
        }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        public virtual string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + Configuration.HttpServerPortNumber + "/" + WebApplicationName + "/";
            }
        }

        /// <summary>
        /// Gets the kernel context. Subclasses will have to override.
        /// </summary>
        /// <value>The kernel context.</value>
        public abstract KernelContext KernelContext { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets or sets the application host.
        /// </summary>
        /// <value>The application host.</value>
        protected IApplicationHost ApplicationHost { get; private set; }

        /// <summary>
        /// The _XML serializer
        /// </summary>
        private readonly IXmlSerializer _xmlSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseKernel{TApplicationPathsType}" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">isoManager</exception>
        protected BaseKernel(IApplicationHost appHost, TApplicationPathsType appPaths, IXmlSerializer xmlSerializer, ILogger logger)
        {
            ApplicationPaths = appPaths;
            ApplicationHost = appHost;
            _xmlSerializer = xmlSerializer;
            Logger = logger;
        }

        /// <summary>
        /// Initializes the Kernel
        /// </summary>
        /// <returns>Task.</returns>
        public void Init()
        {
            ReloadInternal();

            Logger.Info("Kernel.Init Complete");
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual void ReloadInternal()
        {
            ServerManager = ApplicationHost.Resolve<IServerManager>();
        }

        /// <summary>
        /// Notifies that the kernel that a change has been made that requires a restart
        /// </summary>
        public void NotifyPendingRestart()
        {
            HasPendingRestart = true;

            EventHelper.QueueEventIfNotNull(HasPendingRestartChanged, this, EventArgs.Empty, Logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
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
        protected virtual void Dispose(bool dispose)
        {

        }

        /// <summary>
        /// Performs the pending restart.
        /// </summary>
        /// <returns>Task.</returns>
        public void PerformPendingRestart()
        {
            if (HasPendingRestart)
            {
                Logger.Info("Restarting the application");

                ApplicationHost.Restart();
            }
            else
            {
                Logger.Info("PerformPendingRestart - not needed");
            }
        }

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        public virtual SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                HasPendingRestart = HasPendingRestart,
                Version = ApplicationHost.ApplicationVersion.ToString(),
                IsNetworkDeployed = ApplicationHost.CanSelfUpdate,
                WebSocketPortNumber = ServerManager.WebSocketPortNumber,
                SupportsNativeWebSocket = ServerManager.SupportsNativeWebSocket,
                FailedPluginAssemblies = ApplicationHost.FailedAssemblies.ToArray()
            };
        }

        /// <summary>
        /// The _save lock
        /// </summary>
        private readonly object _configurationSaveLock = new object();

        /// <summary>
        /// Saves the current configuration
        /// </summary>
        public void SaveConfiguration()
        {
            lock (_configurationSaveLock)
            {
                _xmlSerializer.SerializeToFile(Configuration, ApplicationPaths.SystemConfigurationFilePath);
            }

            OnConfigurationUpdated();
        }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        IApplicationPaths IKernel.ApplicationPaths
        {
            get { return ApplicationPaths; }
        }
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        BaseApplicationConfiguration IKernel.Configuration
        {
            get { return Configuration; }
        }		        
        
        /// <summary>
        /// Reads an xml configuration file from the file system
        /// It will immediately re-serialize and save if new serialization data is available due to property changes
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.Object.</returns>
        public object GetXmlConfiguration(Type type, string path)
        {
            Logger.Info("Loading {0} at {1}", type.Name, path);

            object configuration;

            byte[] buffer = null;

            // Use try/catch to avoid the extra file system lookup using File.Exists
            try
            {
                buffer = File.ReadAllBytes(path);

                configuration = _xmlSerializer.DeserializeFromBytes(type, buffer);
            }
            catch (FileNotFoundException)
            {
                configuration = Activator.CreateInstance(type);
            }

            // Take the object we just got and serialize it back to bytes
            var newBytes = _xmlSerializer.SerializeToBytes(configuration);

            // If the file didn't exist before, or if something has changed, re-save
            if (buffer == null || !buffer.SequenceEqual(newBytes))
            {
                Logger.Info("Saving {0} to {1}", type.Name, path);

                // Save it after load in case we got new items
                File.WriteAllBytes(path, newBytes);
            }

            return configuration;
        }


        /// <summary>
        /// Reads an xml configuration file from the file system
        /// It will immediately save the configuration after loading it, just
        /// in case there are new serializable properties
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path.</param>
        /// <returns>``0.</returns>
        private T GetXmlConfiguration<T>(string path)
            where T : class
        {
            return GetXmlConfiguration(typeof(T), path) as T;
        }

        /// <summary>
        /// Limits simultaneous access to various resources
        /// </summary>
        /// <value>The resource pools.</value>
        public ResourcePool ResourcePools { get; set; }
    }
}
