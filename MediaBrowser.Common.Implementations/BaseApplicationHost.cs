using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Implementations.Archiving;
using MediaBrowser.Common.Implementations.Devices;
using MediaBrowser.Common.Implementations.IO;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.Implementations.Security;
using MediaBrowser.Common.Implementations.Serialization;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using ServiceStack;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Common.Implementations
{
    /// <summary>
    /// Class BaseApplicationHost
    /// </summary>
    /// <typeparam name="TApplicationPathsType">The type of the T application paths type.</typeparam>
    public abstract class BaseApplicationHost<TApplicationPathsType> : IApplicationHost, IDependencyContainer
        where TApplicationPathsType : class, IApplicationPaths
    {
        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        public event EventHandler HasPendingRestartChanged;

        /// <summary>
        /// Occurs when [application updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<PackageVersionInfo>> ApplicationUpdated;

        /// <summary>
        /// Gets or sets a value indicating whether this instance has changes that require the entire application to restart.
        /// </summary>
        /// <value><c>true</c> if this instance has pending application restart; otherwise, <c>false</c>.</value>
        public bool HasPendingRestart { get; private set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets or sets the plugins.
        /// </summary>
        /// <value>The plugins.</value>
        public IEnumerable<IPlugin> Plugins { get; protected set; }

        /// <summary>
        /// Gets or sets the log manager.
        /// </summary>
        /// <value>The log manager.</value>
        public ILogManager LogManager { get; protected set; }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        protected TApplicationPathsType ApplicationPaths { get; private set; }

        /// <summary>
        /// The container
        /// </summary>
        protected readonly Container Container = new Container();

        /// <summary>
        /// The json serializer
        /// </summary>
        public IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// The _XML serializer
        /// </summary>
        protected readonly IXmlSerializer XmlSerializer;

        /// <summary>
        /// Gets assemblies that failed to load
        /// </summary>
        /// <value>The failed assemblies.</value>
        public List<string> FailedAssemblies { get; protected set; }

        /// <summary>
        /// Gets all concrete types.
        /// </summary>
        /// <value>All concrete types.</value>
        public Type[] AllConcreteTypes { get; protected set; }

        /// <summary>
        /// The disposable parts
        /// </summary>
        protected readonly List<IDisposable> DisposableParts = new List<IDisposable>();

        /// <summary>
        /// Gets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        public bool IsFirstRun { get; private set; }

        /// <summary>
        /// Gets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected ITaskManager TaskManager { get; private set; }
        /// <summary>
        /// Gets the security manager.
        /// </summary>
        /// <value>The security manager.</value>
        protected ISecurityManager SecurityManager { get; private set; }
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        public IHttpClient HttpClient { get; private set; }
        /// <summary>
        /// Gets the network manager.
        /// </summary>
        /// <value>The network manager.</value>
        protected INetworkManager NetworkManager { get; private set; }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        protected IConfigurationManager ConfigurationManager { get; private set; }

        /// <summary>
        /// Gets or sets the installation manager.
        /// </summary>
        /// <value>The installation manager.</value>
        protected IInstallationManager InstallationManager { get; private set; }

        protected IFileSystem FileSystemManager { get; private set; }

        /// <summary>
        /// Gets or sets the zip client.
        /// </summary>
        /// <value>The zip client.</value>
        protected IZipClient ZipClient { get; private set; }

        protected IIsoManager IsoManager { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is running as service.
        /// </summary>
        /// <value><c>true</c> if this instance is running as service; otherwise, <c>false</c>.</value>
        public abstract bool IsRunningAsService { get; }

        private DeviceId _deviceId;
        public string SystemId
        {
            get
            {
                if (_deviceId == null)
                {
                    _deviceId = new DeviceId(ApplicationPaths, LogManager.GetLogger("SystemId"), FileSystemManager);
                }

                return _deviceId.Value;
            }
        }

        public virtual string OperatingSystemDisplayName
        {
            get { return Environment.OSVersion.VersionString; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationHost{TApplicationPathsType}"/> class.
        /// </summary>
        protected BaseApplicationHost(TApplicationPathsType applicationPaths, 
            ILogManager logManager, 
            IFileSystem fileSystem)
        {
			XmlSerializer = new XmlSerializer (fileSystem, logManager.GetLogger("XmlSerializer"));
            FailedAssemblies = new List<string>();

            ApplicationPaths = applicationPaths;
            LogManager = logManager;
            FileSystemManager = fileSystem;

            ConfigurationManager = GetConfigurationManager();

            // Initialize this early in case the -v command line option is used
            Logger = LogManager.GetLogger("App");
        }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public virtual async Task Init(IProgress<double> progress)
        {
            try
            {
                // https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.IntegrationTests/Web.config#L4
                Licensing.RegisterLicense("1001-e1JlZjoxMDAxLE5hbWU6VGVzdCBCdXNpbmVzcyxUeXBlOkJ1c2luZXNzLEhhc2g6UHVNTVRPclhvT2ZIbjQ5MG5LZE1mUTd5RUMzQnBucTFEbTE3TDczVEF4QUNMT1FhNXJMOWkzVjFGL2ZkVTE3Q2pDNENqTkQyUktRWmhvUVBhYTBiekJGUUZ3ZE5aZHFDYm9hL3lydGlwUHI5K1JsaTBYbzNsUC85cjVJNHE5QVhldDN6QkE4aTlvdldrdTgyTk1relY2eis2dFFqTThYN2lmc0JveHgycFdjPSxFeHBpcnk6MjAxMy0wMS0wMX0=");
            }
            catch
            {
                // Failing under mono
            }
            progress.Report(1);

            JsonSerializer = CreateJsonSerializer();

            OnLoggerLoaded(true);
            LogManager.LoggerLoaded += (s, e) => OnLoggerLoaded(false);

            IsFirstRun = !ConfigurationManager.CommonConfiguration.IsStartupWizardCompleted;
            progress.Report(2);

            LogManager.LogSeverity = ConfigurationManager.CommonConfiguration.EnableDebugLevelLogging
                                         ? LogSeverity.Debug
                                         : LogSeverity.Info;

            progress.Report(3);

            DiscoverTypes();
            progress.Report(14);

            SetHttpLimit();
            progress.Report(15);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(.8 * p + 15));

            await RegisterResources(innerProgress).ConfigureAwait(false);

            FindParts();
            progress.Report(95);

            await InstallIsoMounters(CancellationToken.None).ConfigureAwait(false);

            progress.Report(100);
        }

        protected virtual void OnLoggerLoaded(bool isFirstLoad)
        {
            Logger.Info("Application version: {0}", ApplicationVersion);

            if (!isFirstLoad)
            {
                LogEnvironmentInfo(Logger, ApplicationPaths, false);
            }

            // Put the app config in the log for troubleshooting purposes
            Logger.LogMultiline("Application configuration:", LogSeverity.Info, new StringBuilder(JsonSerializer.SerializeToString(ConfigurationManager.CommonConfiguration)));

            if (Plugins != null)
            {
                var pluginBuilder = new StringBuilder();

                foreach (var plugin in Plugins)
                {
                    pluginBuilder.AppendLine(string.Format("{0} {1}", plugin.Name, plugin.Version));
                }

                Logger.LogMultiline("Plugins:", LogSeverity.Info, pluginBuilder);
            }
        }

        public static void LogEnvironmentInfo(ILogger logger, IApplicationPaths appPaths, bool isStartup)
        {
            logger.LogMultiline("Emby", LogSeverity.Info, GetBaseExceptionMessage(appPaths));
        }

        protected static StringBuilder GetBaseExceptionMessage(IApplicationPaths appPaths)
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.Format("Command line: {0}", string.Join(" ", Environment.GetCommandLineArgs())));

            builder.AppendLine(string.Format("Operating system: {0}", Environment.OSVersion));
            builder.AppendLine(string.Format("Processor count: {0}", Environment.ProcessorCount));
            builder.AppendLine(string.Format("64-Bit OS: {0}", Environment.Is64BitOperatingSystem));
            builder.AppendLine(string.Format("64-Bit Process: {0}", Environment.Is64BitProcess));
            builder.AppendLine(string.Format("Program data path: {0}", appPaths.ProgramDataPath));

            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    builder.AppendLine("Mono: " + displayName.Invoke(null, null));
                }
            }

            builder.AppendLine(string.Format("Application Path: {0}", appPaths.ApplicationPath));

            return builder;
        }

        protected virtual IJsonSerializer CreateJsonSerializer()
        {
            return new JsonSerializer(FileSystemManager, LogManager.GetLogger("JsonSerializer"));
        }

        private void SetHttpLimit()
        {
            try
            {
                // Increase the max http request limit
                ServicePointManager.DefaultConnectionLimit = Math.Max(96, ServicePointManager.DefaultConnectionLimit);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error setting http limit", ex);
            }
        }

        /// <summary>
        /// Installs the iso mounters.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task InstallIsoMounters(CancellationToken cancellationToken)
        {
            var list = new List<IIsoMounter>();

            foreach (var isoMounter in GetExports<IIsoMounter>())
            {
                try
                {
                    if (isoMounter.RequiresInstallation && !isoMounter.IsInstalled)
                    {
                        Logger.Info("Installing {0}", isoMounter.Name);

                        await isoMounter.Install(cancellationToken).ConfigureAwait(false);
                    }

                    list.Add(isoMounter);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("{0} failed to load.", ex, isoMounter.Name);
                }
            }

            IsoManager.AddParts(list);
        }

        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        /// <returns>Task.</returns>
        public virtual Task RunStartupTasks()
        {
			Resolve<ITaskManager>().AddTasks(GetExports<IScheduledTask>(false));

			ConfigureAutorun ();

			ConfigurationManager.ConfigurationUpdated += OnConfigurationUpdated;

			return Task.FromResult (true);
        }

        /// <summary>
        /// Configures the autorun.
        /// </summary>
        private void ConfigureAutorun()
        {
            try
            {
                ConfigureAutoRunAtStartup(ConfigurationManager.CommonConfiguration.RunAtStartup);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error configuring autorun", ex);
            }
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected abstract IEnumerable<Assembly> GetComposablePartAssemblies();

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <returns>IConfigurationManager.</returns>
        protected abstract IConfigurationManager GetConfigurationManager();

        /// <summary>
        /// Finds the parts.
        /// </summary>
        protected virtual void FindParts()
        {
            RegisterModules();
            
            ConfigurationManager.AddParts(GetExports<IConfigurationFactory>());
            Plugins = GetExports<IPlugin>();
        }

        /// <summary>
        /// Discovers the types.
        /// </summary>
        protected void DiscoverTypes()
        {
            FailedAssemblies.Clear();

            var assemblies = GetComposablePartAssemblies().ToList();

            foreach (var assembly in assemblies)
            {
                Logger.Info("Loading {0}", assembly.FullName);
            }

            AllConcreteTypes = assemblies
                .SelectMany(GetTypes)
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && !t.IsGenericType)
                .ToArray();
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task RegisterResources(IProgress<double> progress)
        {
			RegisterSingleInstance(ConfigurationManager);
			RegisterSingleInstance<IApplicationHost>(this);

			RegisterSingleInstance<IApplicationPaths>(ApplicationPaths);

			TaskManager = new TaskManager(ApplicationPaths, JsonSerializer, LogManager.GetLogger("TaskManager"), FileSystemManager);

			RegisterSingleInstance(JsonSerializer);
			RegisterSingleInstance(XmlSerializer);

			RegisterSingleInstance(LogManager);
			RegisterSingleInstance(Logger);

			RegisterSingleInstance(TaskManager);

			RegisterSingleInstance(FileSystemManager);

            HttpClient = new HttpClientManager.HttpClientManager(ApplicationPaths, LogManager.GetLogger("HttpClient"), FileSystemManager);
			RegisterSingleInstance(HttpClient);

			NetworkManager = CreateNetworkManager(LogManager.GetLogger("NetworkManager"));
			RegisterSingleInstance(NetworkManager);

			SecurityManager = new PluginSecurityManager(this, HttpClient, JsonSerializer, ApplicationPaths, LogManager);
			RegisterSingleInstance(SecurityManager);

            InstallationManager = new InstallationManager(LogManager.GetLogger("InstallationManager"), this, ApplicationPaths, HttpClient, JsonSerializer, SecurityManager, ConfigurationManager, FileSystemManager);
			RegisterSingleInstance(InstallationManager);

			ZipClient = new ZipClient(FileSystemManager);
			RegisterSingleInstance(ZipClient);

			IsoManager = new IsoManager();
			RegisterSingleInstance(IsoManager);

			return Task.FromResult (true);
        }

        private void RegisterModules()
        {
            var moduleTypes = GetExportTypes<IDependencyModule>();

            foreach (var type in moduleTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as IDependencyModule;
                    if (instance != null)
                        instance.BindDependencies(this);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error setting up dependency bindings for " + type.Name, ex);
                }
            }
        }

        /// <summary>
        /// Gets a list of types within an assembly
        /// This will handle situations that would normally throw an exception - such as a type within the assembly that depends on some other non-existant reference
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>IEnumerable{Type}.</returns>
        /// <exception cref="System.ArgumentNullException">assembly</exception>
        protected IEnumerable<Type> GetTypes(Assembly assembly)
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
                if (ex.LoaderExceptions != null)
                {
                    foreach (var loaderException in ex.LoaderExceptions)
                    {
                        Logger.Error("LoaderException: " + loaderException.Message);
                    }
                }
                
                // If it fails we can still get a list of the Types it was able to resolve
                return ex.Types.Where(t => t != null);
            }
        }

        protected abstract INetworkManager CreateNetworkManager(ILogger logger);

        /// <summary>
        /// Creates an instance of type and resolves all constructor dependancies
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        public object CreateInstance(Type type)
        {
            try
            {
                return Container.GetInstance(type);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error creating {0}", ex, type.Name);

                throw;
            }
        }

        /// <summary>
        /// Creates the instance safe.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        protected object CreateInstanceSafe(Type type)
        {
            try
            {
                return Container.GetInstance(type);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error creating {0}", ex, type.Name);
                // Don't blow up in release mode
                return null;
            }
        }

        void IDependencyContainer.RegisterSingleInstance<T>(T obj, bool manageLifetime)
        {
            RegisterSingleInstance(obj, manageLifetime);
        }

        /// <summary>
        /// Registers the specified obj.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <param name="manageLifetime">if set to <c>true</c> [manage lifetime].</param>
        protected void RegisterSingleInstance<T>(T obj, bool manageLifetime = true)
            where T : class
        {
            Container.RegisterSingleton(obj);

            if (manageLifetime)
            {
                var disposable = obj as IDisposable;

                if (disposable != null)
                {
                    DisposableParts.Add(disposable);
                }
            }
        }

        void IDependencyContainer.RegisterSingleInstance<T>(Func<T> func)
        {
            RegisterSingleInstance(func);
        }

        /// <summary>
        /// Registers the single instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">The func.</param>
        protected void RegisterSingleInstance<T>(Func<T> func)
            where T : class
        {
            Container.RegisterSingleton(func);
        }

        void IDependencyContainer.Register(Type typeInterface, Type typeImplementation)
        {
            Container.Register(typeInterface, typeImplementation);
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T Resolve<T>()
        {
            return (T)Container.GetRegistration(typeof(T), true).GetInstance();
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T TryResolve<T>()
        {
            var result = Container.GetRegistration(typeof(T), false);

            if (result == null)
            {
                return default(T);
            }
            return (T)result.GetInstance();
        }

        /// <summary>
        /// Loads the assembly.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>Assembly.</returns>
        protected Assembly LoadAssembly(string file)
        {
            try
            {
                return Assembly.Load(File.ReadAllBytes(file));
            }
            catch (Exception ex)
            {
                FailedAssemblies.Add(file);
                Logger.ErrorException("Error loading assembly {0}", ex, file);
                return null;
            }
        }

        /// <summary>
        /// Gets the export types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>IEnumerable{Type}.</returns>
        public IEnumerable<Type> GetExportTypes<T>()
        {
            var currentType = typeof(T);

            return AllConcreteTypes.AsParallel().Where(currentType.IsAssignableFrom);
        }

        /// <summary>
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manageLiftime">if set to <c>true</c> [manage liftime].</param>
        /// <returns>IEnumerable{``0}.</returns>
        public IEnumerable<T> GetExports<T>(bool manageLiftime = true)
        {
            var parts = GetExportTypes<T>()
                .Select(CreateInstanceSafe)
                .Where(i => i != null)
                .Cast<T>()
                .ToList();

            if (manageLiftime)
            {
                lock (DisposableParts)
                {
                    DisposableParts.AddRange(parts.OfType<IDisposable>());
                }
            }

            return parts;
        }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>The application version.</value>
        public abstract Version ApplicationVersion { get; }

        /// <summary>
        /// Handles the ConfigurationUpdated event of the ConfigurationManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual void OnConfigurationUpdated(object sender, EventArgs e)
        {
            ConfigureAutorun();
        }

        protected abstract void ConfigureAutoRunAtStartup(bool autorun);

        /// <summary>
        /// Removes the plugin.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        public void RemovePlugin(IPlugin plugin)
        {
            var list = Plugins.ToList();
            list.Remove(plugin);
            Plugins = list;
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public abstract bool CanSelfRestart { get; }

        /// <summary>
        /// Notifies that the kernel that a change has been made that requires a restart
        /// </summary>
        public void NotifyPendingRestart()
        {
            var changed = !HasPendingRestart;

            HasPendingRestart = true;

            if (changed)
            {
                EventHelper.QueueEventIfNotNull(HasPendingRestartChanged, this, EventArgs.Empty, Logger);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                var type = GetType();

                Logger.Info("Disposing " + type.Name);

                var parts = DisposableParts.Distinct().Where(i => i.GetType() != type).ToList();
                DisposableParts.Clear();

                foreach (var part in parts)
                {
                    Logger.Info("Disposing " + part.GetType().Name);

                    try
                    {
                        part.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error disposing {0}", ex, part.GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public abstract Task Restart();

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public abstract bool CanSelfUpdate { get; }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public abstract Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken,
                                                                          IProgress<double> progress);

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="package">The package that contains the update</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public abstract Task UpdateApplication(PackageVersionInfo package, CancellationToken cancellationToken,
                                            IProgress<double> progress);

        /// <summary>
        /// Shuts down.
        /// </summary>
        public abstract Task Shutdown();

        /// <summary>
        /// Called when [application updated].
        /// </summary>
        /// <param name="package">The package.</param>
        protected void OnApplicationUpdated(PackageVersionInfo package)
        {
            Logger.Info("Application has been updated to version {0}", package.versionStr);

            EventHelper.FireEventIfNotNull(ApplicationUpdated, this, new GenericEventArgs<PackageVersionInfo>
            {
                Argument = package

            }, Logger);

            NotifyPendingRestart();
        }
    }
}
