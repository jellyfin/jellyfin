using MediaBrowser.Common.Events;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Localization;
using MediaBrowser.Common.Mef;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Deployment.Application;
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

        /// <summary>
        /// Notifiies the containing application that a restart has been requested
        /// </summary>
        public event EventHandler ApplicationRestartRequested;

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
            EventHelper.QueueEventIfNotNull(ConfigurationUpdated, this, EventArgs.Empty);

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
            EventHelper.QueueEventIfNotNull(LoggerLoaded, this, EventArgs.Empty);
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
            EventHelper.QueueEventIfNotNull(ReloadBeginning, this, EventArgs.Empty);
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
            EventHelper.QueueEventIfNotNull(ReloadCompleted, this, EventArgs.Empty);
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
            EventHelper.QueueEventIfNotNull(ApplicationUpdated, this, new GenericEventArgs<Version> { Argument = newVersion });

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
        /// The version of the application to display
        /// </summary>
        /// <value>The display version.</value>
        public string DisplayVersion { get { return ApplicationVersion.ToString(); } }

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
        [ImportMany(typeof(IPlugin))]
        public IEnumerable<IPlugin> Plugins { get; protected set; }

        /// <summary>
        /// Gets the list of Scheduled Tasks
        /// </summary>
        /// <value>The scheduled tasks.</value>
        [ImportMany(typeof(IScheduledTask))]
        public IEnumerable<IScheduledTask> ScheduledTasks { get; private set; }

        /// <summary>
        /// Gets the web socket listeners.
        /// </summary>
        /// <value>The web socket listeners.</value>
        [ImportMany(typeof(IWebSocketListener))]
        public IEnumerable<IWebSocketListener> WebSocketListeners { get; private set; }

        /// <summary>
        /// Gets the list of Localized string files
        /// </summary>
        /// <value>The string files.</value>
        [ImportMany(typeof(LocalizedStringData))]
        public IEnumerable<LocalizedStringData> StringFiles { get; private set; }

        /// <summary>
        /// Gets the MEF CompositionContainer
        /// </summary>
        /// <value>The composition container.</value>
        private CompositionContainer CompositionContainer { get; set; }

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
        /// Gets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        public TaskManager TaskManager { get; private set; }

        /// <summary>
        /// Gets the iso manager.
        /// </summary>
        /// <value>The iso manager.</value>
        public IIsoManager IsoManager { get; private set; }

        /// <summary>
        /// Gets the rest services.
        /// </summary>
        /// <value>The rest services.</value>
        [ImportMany(typeof(IRestfulService))]
        public IEnumerable<IRestfulService> RestServices { get; private set; }

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
                LazyInitializer.EnsureInitialized(ref _protobufSerializer, ref _protobufSerializerInitialized, ref _protobufSerializerSyncLock, () => DynamicProtobufSerializer.Create(Assemblies));
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
        public string LogFilePath { get; private set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets the assemblies.
        /// </summary>
        /// <value>The assemblies.</value>
        public Assembly[] Assemblies { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseKernel{TApplicationPathsType}" /> class.
        /// </summary>
        /// <param name="isoManager">The iso manager.</param>
        protected BaseKernel(IIsoManager isoManager)
        {
            IsoManager = isoManager;
        }

        /// <summary>
        /// Initializes the Kernel
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Init()
        {
            Logger = Logging.LogManager.GetLogger(GetType().Name);

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
            HttpManager = new HttpManager(this);

            await OnConfigurationLoaded().ConfigureAwait(false);

            DisposeTaskManager();
            TaskManager = new TaskManager(this);

            Logger.Info("Loading Plugins");
            await ReloadComposableParts().ConfigureAwait(false);

            DisposeTcpManager();
            TcpManager = new TcpManager(this);
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
            DisposeLogger();

            LogFilePath = Path.Combine(ApplicationPaths.LogDirectoryPath, KernelContext + "-" + DateTime.Now.Ticks + ".log");

            var logFile = new FileTarget();

            logFile.FileName = LogFilePath;
            logFile.Layout = "${longdate}, ${level}, ${logger}, ${message}";

            AddLogTarget(logFile, "ApplicationLogFile");

            OnLoggerLoaded();
        }

        /// <summary>
        /// Adds the log target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        private void AddLogTarget(Target target, string name)
        {
            var config = LogManager.Configuration;

            config.RemoveTarget(name);

            target.Name = name;
            config.AddTarget(name, target);

            var level = Configuration.EnableDebugLevelLogging ? LogLevel.Debug : LogLevel.Info;

            var rule = new LoggingRule("*", level, target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
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

            CompositionContainer = MefUtils.GetSafeCompositionContainer(Assemblies.Select(i => new AssemblyCatalog(i)));

            ComposeExportedValues(CompositionContainer);

            CompositionContainer.ComposeParts(this);

            await OnComposablePartsLoaded().ConfigureAwait(false);

            CompositionContainer.Catalog.Dispose();
        }

        /// <summary>
        /// Composes the exported values.
        /// </summary>
        /// <param name="container">The container.</param>
        protected virtual void ComposeExportedValues(CompositionContainer container)
        {
            container.ComposeExportedValue("logger", Logging.LogManager.GetLogger("App"));
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
        /// Fires after MEF finishes finding composable parts within plugin assemblies
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task OnComposablePartsLoaded()
        {
            return Task.Run(() =>
            {
                foreach (var listener in WebSocketListeners)
                {
                    listener.Initialize(this);
                }

                foreach (var task in ScheduledTasks)
                {
                    task.Initialize(this);
                }

                // Start-up each plugin
                Parallel.ForEach(Plugins, plugin =>
                {
                    Logger.Info("Initializing {0} {1}", plugin.Name, plugin.Version);

                    try
                    {
                        plugin.Initialize(this, Logging.LogManager.GetLogger(plugin.GetType().Name));

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

            EventHelper.QueueEventIfNotNull(HasPendingRestartChanged, this, EventArgs.Empty);
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
                DisposeIsoManager();
                DisposeHttpManager();

                DisposeComposableParts();
            }
        }

        /// <summary>
        /// Disposes the iso manager.
        /// </summary>
        private void DisposeIsoManager()
        {
            if (IsoManager != null)
            {
                IsoManager.Dispose();
                IsoManager = null;
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
            if (CompositionContainer != null)
            {
                CompositionContainer.Dispose();
            }
        }

        /// <summary>
        /// Disposes all logger resources
        /// </summary>
        private void DisposeLogger()
        {
            // Dispose all current loggers
            var listeners = Trace.Listeners.OfType<TraceListener>().ToList();

            Trace.Listeners.Clear();

            foreach (var listener in listeners)
            {
                listener.Dispose();
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

            EventHelper.QueueEventIfNotNull(ApplicationRestartRequested, this, EventArgs.Empty);
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
                Version = DisplayVersion,
                IsNetworkDeployed = ApplicationDeployment.IsNetworkDeployed,
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
