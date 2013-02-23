using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Represents a shared base kernel for both the Ui and server apps
    /// </summary>
    /// <typeparam name="TConfigurationType">The type of the T configuration type.</typeparam>
    /// <typeparam name="TApplicationPathsType">The type of the T application paths type.</typeparam>
    public abstract class BaseKernel<TConfigurationType, TApplicationPathsType> : IDisposable, IKernel
        where TConfigurationType : BaseApplicationConfiguration, new()
        where TApplicationPathsType : BaseApplicationPaths, new()
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

            // Notify connected clients
            TcpManager.SendWebSocketMessage("ConfigurationUpdated", Configuration);
        }
        #endregion

        #region LoggerLoaded Event
        /// <summary>
        /// Fires whenever the logger is loaded
        /// </summary>
        public event EventHandler LoggerLoaded;
        /// <summary>
        /// Called when [logger loaded].
        /// </summary>
        private void OnLoggerLoaded()
        {
            EventHelper.QueueEventIfNotNull(LoggerLoaded, this, EventArgs.Empty, Logger);
        }
        #endregion

        #region ReloadBeginning Event
        /// <summary>
        /// Fires whenever the kernel begins reloading
        /// </summary>
        public event EventHandler<EventArgs> ReloadBeginning;
        /// <summary>
        /// Called when [reload beginning].
        /// </summary>
        private void OnReloadBeginning()
        {
            EventHelper.QueueEventIfNotNull(ReloadBeginning, this, EventArgs.Empty, Logger);
        }
        #endregion

        #region ReloadCompleted Event
        /// <summary>
        /// Fires whenever the kernel completes reloading
        /// </summary>
        public event EventHandler<EventArgs> ReloadCompleted;
        /// <summary>
        /// Called when [reload completed].
        /// </summary>
        private void OnReloadCompleted()
        {
            EventHelper.QueueEventIfNotNull(ReloadCompleted, this, EventArgs.Empty, Logger);
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
                LazyInitializer.EnsureInitialized(ref _configuration, ref _configurationLoaded, ref _configurationSyncLock, () => XmlSerializer.GetXmlConfiguration<TConfigurationType>(ApplicationPaths.SystemConfigurationFilePath, Logger));
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
        /// Gets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        public bool IsFirstRun { get; private set; }

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
        /// The _failed assembly loads
        /// </summary>
        private readonly List<string> _failedPluginAssemblies = new List<string>();
        /// <summary>
        /// Gets the plugin assemblies that failed to load.
        /// </summary>
        /// <value>The failed assembly loads.</value>
        public IEnumerable<string> FailedPluginAssemblies
        {
            get { return _failedPluginAssemblies; }
        }

        /// <summary>
        /// Gets the list of currently loaded plugins
        /// </summary>
        /// <value>The plugins.</value>
        public IEnumerable<IPlugin> Plugins { get; protected set; }

        /// <summary>
        /// Gets the web socket listeners.
        /// </summary>
        /// <value>The web socket listeners.</value>
        public IEnumerable<IWebSocketListener> WebSocketListeners { get; private set; }

        /// <summary>
        /// The _HTTP manager
        /// </summary>
        /// <value>The HTTP manager.</value>
        public HttpManager HttpManager { get; private set; }

        /// <summary>
        /// Gets or sets the TCP manager.
        /// </summary>
        /// <value>The TCP manager.</value>
        public TcpManager TcpManager { get; private set; }

        /// <summary>
        /// Gets the rest services.
        /// </summary>
        /// <value>The rest services.</value>
        public IEnumerable<IRestfulService> RestServices { get; private set; }

        /// <summary>
        /// The disposable parts
        /// </summary>
        private readonly List<IDisposable> _disposableParts = new List<IDisposable>();

        /// <summary>
        /// The _protobuf serializer initialized
        /// </summary>
        private bool _protobufSerializerInitialized;
        /// <summary>
        /// The _protobuf serializer sync lock
        /// </summary>
        private object _protobufSerializerSyncLock = new object();
        /// <summary>
        /// Gets a dynamically compiled generated serializer that can serialize protocontracts without reflection
        /// </summary>
        private DynamicProtobufSerializer _protobufSerializer;
        /// <summary>
        /// Gets the protobuf serializer.
        /// </summary>
        /// <value>The protobuf serializer.</value>
        public DynamicProtobufSerializer ProtobufSerializer
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _protobufSerializer, ref _protobufSerializerInitialized, ref _protobufSerializerSyncLock, () => DynamicProtobufSerializer.Create(AllTypes));
                return _protobufSerializer;
            }
            private set
            {
                _protobufSerializer = value;

                if (value == null)
                {
                    _protobufSerializerInitialized = false;
                }
            }
        }

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
        /// Gets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        public string LogFilePath
        {
            get { return ApplicationHost.LogFilePath; }
        }

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
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        protected ITaskManager TaskManager { get; set; }

        /// <summary>
        /// Gets the assemblies.
        /// </summary>
        /// <value>The assemblies.</value>
        protected Assembly[] Assemblies { get; private set; }

        /// <summary>
        /// Gets all types.
        /// </summary>
        /// <value>All types.</value>
        public Type[] AllTypes { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseKernel{TApplicationPathsType}" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">isoManager</exception>
        protected BaseKernel(IApplicationHost appHost, ILogger logger)
        {
            if (appHost == null)
            {
                throw new ArgumentNullException("appHost");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            ApplicationHost = appHost;
            Logger = logger;
        }

        /// <summary>
        /// Initializes the Kernel
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Init()
        {
            ApplicationPaths = new TApplicationPathsType();

            IsFirstRun = !File.Exists(ApplicationPaths.SystemConfigurationFilePath);

            // Performs initializations that can be reloaded at anytime
            await Reload().ConfigureAwait(false);
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Reload()
        {
            OnReloadBeginning();

            await ReloadInternal().ConfigureAwait(false);

            OnReloadCompleted();

            Logger.Info("Kernel.Reload Complete");
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual async Task ReloadInternal()
        {
            // Set these to null so that they can be lazy loaded again
            Configuration = null;
            ProtobufSerializer = null;

            ReloadLogger();

            Logger.Info("Version {0} initializing", ApplicationVersion);

            DisposeHttpManager();
            HttpManager = new HttpManager(this, Logger);

            await OnConfigurationLoaded().ConfigureAwait(false);

            DisposeTaskManager();
            TaskManager = new TaskManager(Logger);

            Logger.Info("Loading Plugins");
            await ReloadComposableParts().ConfigureAwait(false);

            DisposeTcpManager();
            TcpManager = new TcpManager(ApplicationHost, this, ApplicationHost.Resolve<INetworkManager>(), Logger);
        }

        /// <summary>
        /// Called when [configuration loaded].
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task OnConfigurationLoaded()
        {
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Disposes and reloads all loggers
        /// </summary>
        public void ReloadLogger()
        {
            ApplicationHost.ReloadLogger();
            
            OnLoggerLoaded();
        }

        /// <summary>
        /// Uses MEF to locate plugins
        /// Subclasses can use this to locate types within plugins
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ReloadComposableParts()
        {
            _failedPluginAssemblies.Clear();

            DisposeComposableParts();

            Assemblies = GetComposablePartAssemblies().ToArray();

            AllTypes = Assemblies.SelectMany(GetTypes).ToArray();

            ComposeParts(AllTypes);

            await OnComposablePartsLoaded().ConfigureAwait(false);
        }

        /// <summary>
        /// Composes the parts.
        /// </summary>
        /// <param name="allTypes">All types.</param>
        private void ComposeParts(IEnumerable<Type> allTypes)
        {
            var concreteTypes = allTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && !t.IsGenericType).ToArray();

            RegisterExportedValues();

            FindParts(concreteTypes);
        }

        /// <summary>
        /// Composes the parts with ioc container.
        /// </summary>
        /// <param name="allTypes">All types.</param>
        protected virtual void FindParts(Type[] allTypes)
        {
            RestServices = GetExports<IRestfulService>(allTypes);
            WebSocketListeners = GetExports<IWebSocketListener>(allTypes);
            Plugins = GetExports<IPlugin>(allTypes);

            var tasks = GetExports<IScheduledTask>(allTypes, false);

            TaskManager.AddTasks(tasks);
        }

        /// <summary>
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="allTypes">All types.</param>
        /// <param name="manageLiftime">if set to <c>true</c> [manage liftime].</param>
        /// <returns>IEnumerable{``0}.</returns>
        protected IEnumerable<T> GetExports<T>(Type[] allTypes, bool manageLiftime = true)
        {
            var currentType = typeof(T);

            Logger.Info("Composing instances of " + currentType.Name);

            var parts = allTypes.Where(currentType.IsAssignableFrom).Select(Instantiate).Cast<T>().ToArray();

            if (manageLiftime)
            {
                _disposableParts.AddRange(parts.OfType<IDisposable>());
            }

            return parts;
        }

        /// <summary>
        /// Instantiates the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        private object Instantiate(Type type)
        {
            return ApplicationHost.CreateInstance(type);
        }

        /// <summary>
        /// Composes the exported values.
        /// </summary>
        /// <param name="container">The container.</param>
        protected virtual void RegisterExportedValues()
        {
            ApplicationHost.Register<IKernel>(this);
            ApplicationHost.Register(TaskManager);
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected virtual IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            var pluginAssemblies = Directory.EnumerateFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(file =>
                {
                    try
                    {
                        return Assembly.Load(File.ReadAllBytes((file)));
                    }
                    catch (Exception ex)
                    {
                        _failedPluginAssemblies.Add(file);
                        Logger.ErrorException("Error loading {0}", ex, file);
                        return null;
                    }

                }).Where(a => a != null);

            foreach (var pluginAssembly in pluginAssemblies)
            {
                yield return pluginAssembly;
            }

            var runningDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            var corePluginDirectory = Path.Combine(runningDirectory, "CorePlugins");

            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            pluginAssemblies = Directory.EnumerateFiles(corePluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(file =>
                {
                    try
                    {
                        return Assembly.Load(File.ReadAllBytes((file)));
                    }
                    catch (Exception ex)
                    {
                        _failedPluginAssemblies.Add(file);
                        Logger.ErrorException("Error loading {0}", ex, file);
                        return null;
                    }

                }).Where(a => a != null);

            foreach (var pluginAssembly in pluginAssemblies)
            {
                yield return pluginAssembly;
            }

            // Include composable parts in the Model assembly 
            yield return typeof(SystemInfo).Assembly;

            // Include composable parts in the Common assembly 
            yield return Assembly.GetExecutingAssembly();

            // Include composable parts in the subclass assembly
            yield return GetType().Assembly;
        }

        /// <summary>
        /// Gets a list of types within an assembly
        /// This will handle situations that would normally throw an exception - such as a type within the assembly that depends on some other non-existant reference
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>IEnumerable{Type}.</returns>
        /// <exception cref="System.ArgumentNullException">assembly</exception>
        private static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // If it fails we can still get a list of the Types it was able to resolve
                return ex.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// Fires after MEF finishes finding composable parts within plugin assemblies
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task OnComposablePartsLoaded()
        {
            return Task.Run(() =>
            {
                // Start-up each plugin
                Parallel.ForEach(Plugins, plugin =>
                {
                    Logger.Info("Initializing {0} {1}", plugin.Name, plugin.Version);

                    try
                    {
                        plugin.Initialize(this, Logger);

                        Logger.Info("{0} {1} initialized.", plugin.Name, plugin.Version);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error initializing {0}", ex, plugin.Name);
                    }
                });
            });
        }

        /// <summary>
        /// Notifies that the kernel that a change has been made that requires a restart
        /// </summary>
        public void NotifyPendingRestart()
        {
            HasPendingRestart = true;

            TcpManager.SendWebSocketMessage("HasPendingRestartChanged", GetSystemInfo());

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
            if (dispose)
            {
                DisposeTcpManager();
                DisposeTaskManager();
                DisposeHttpManager();

                DisposeComposableParts();

                _disposableParts.Clear();
            }
        }

        /// <summary>
        /// Disposes the TCP manager.
        /// </summary>
        private void DisposeTcpManager()
        {
            if (TcpManager != null)
            {
                TcpManager.Dispose();
                TcpManager = null;
            }
        }

        /// <summary>
        /// Disposes the task manager.
        /// </summary>
        private void DisposeTaskManager()
        {
            if (TaskManager != null)
            {
                TaskManager.Dispose();
                TaskManager = null;
            }
        }

        /// <summary>
        /// Disposes the HTTP manager.
        /// </summary>
        private void DisposeHttpManager()
        {
            if (HttpManager != null)
            {
                HttpManager.Dispose();
                HttpManager = null;
            }
        }

        /// <summary>
        /// Disposes all objects gathered through MEF composable parts
        /// </summary>
        protected virtual void DisposeComposableParts()
        {
            foreach (var part in _disposableParts)
            {
                part.Dispose();
            }
        }

        /// <summary>
        /// Gets the current application version
        /// </summary>
        /// <value>The application version.</value>
        public Version ApplicationVersion
        {
            get
            {
                return GetType().Assembly.GetName().Version;
            }
        }

        /// <summary>
        /// Performs the pending restart.
        /// </summary>
        /// <returns>Task.</returns>
        public void PerformPendingRestart()
        {
            if (HasPendingRestart)
            {
                RestartApplication();
            }
            else
            {
                Logger.Info("PerformPendingRestart - not needed");
            }
        }

        /// <summary>
        /// Restarts the application.
        /// </summary>
        protected void RestartApplication()
        {
            Logger.Info("Restarting the application");

            ApplicationHost.Restart();
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
                Version = ApplicationVersion.ToString(),
                IsNetworkDeployed = ApplicationHost.CanSelfUpdate,
                WebSocketPortNumber = TcpManager.WebSocketPortNumber,
                SupportsNativeWebSocket = TcpManager.SupportsNativeWebSocket,
                FailedPluginAssemblies = FailedPluginAssemblies.ToArray()
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
                XmlSerializer.SerializeToFile(Configuration, ApplicationPaths.SystemConfigurationFilePath);
            }

            OnConfigurationUpdated();
        }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        BaseApplicationPaths IKernel.ApplicationPaths
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
    }
}
