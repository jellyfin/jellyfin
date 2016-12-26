using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using Emby.Common.Implementations.Devices;
using Emby.Common.Implementations.IO;
using Emby.Common.Implementations.ScheduledTasks;
using Emby.Common.Implementations.Serialization;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using Emby.Common.Implementations.Cryptography;
using Emby.Common.Implementations.Diagnostics;
using Emby.Common.Implementations.Net;
using Emby.Common.Implementations.EnvironmentInfo;
using Emby.Common.Implementations.Threading;
using MediaBrowser.Common;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Threading;

#if NETSTANDARD1_6
using System.Runtime.Loader;
#endif

namespace Emby.Common.Implementations
{
    /// <summary>
    /// Class BaseApplicationHost
    /// </summary>
    /// <typeparam name="TApplicationPathsType">The type of the T application paths type.</typeparam>
    public abstract class BaseApplicationHost<TApplicationPathsType> : IApplicationHost
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
        public IPlugin[] Plugins { get; protected set; }

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

        protected IFileSystem FileSystemManager { get; private set; }

        protected IIsoManager IsoManager { get; private set; }

        protected IProcessFactory ProcessFactory { get; private set; }
        protected ITimerFactory TimerFactory { get; private set; }
        protected ISocketFactory SocketFactory { get; private set; }

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

        protected ICryptoProvider CryptographyProvider = new CryptographyProvider();

        protected IEnvironmentInfo EnvironmentInfo { get; private set; }

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
            get { return EnvironmentInfo.OperatingSystemName; }
        }

        /// <summary>
        /// The container
        /// </summary>
        protected readonly SimpleInjector.Container Container = new SimpleInjector.Container();

        protected ISystemEvents SystemEvents { get; private set; }
        protected IMemoryStreamFactory MemoryStreamFactory { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationHost{TApplicationPathsType}"/> class.
        /// </summary>
        protected BaseApplicationHost(TApplicationPathsType applicationPaths,
            ILogManager logManager,
            IFileSystem fileSystem,
            IEnvironmentInfo environmentInfo,
            ISystemEvents systemEvents,
            IMemoryStreamFactory memoryStreamFactory,
            INetworkManager networkManager)
        {
            NetworkManager = networkManager;
            EnvironmentInfo = environmentInfo;
            SystemEvents = systemEvents;
            MemoryStreamFactory = memoryStreamFactory;

            // hack alert, until common can target .net core
            BaseExtensions.CryptographyProvider = CryptographyProvider;
            
            XmlSerializer = new MyXmlSerializer(fileSystem, logManager.GetLogger("XmlSerializer"));
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

#if NET46
            builder.AppendLine(string.Format("Operating system: {0}", Environment.OSVersion));
            builder.AppendLine(string.Format("64-Bit OS: {0}", Environment.Is64BitOperatingSystem));
            builder.AppendLine(string.Format("64-Bit Process: {0}", Environment.Is64BitProcess));

            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    builder.AppendLine("Mono: " + displayName.Invoke(null, null));
                }
            }
#endif    

            builder.AppendLine(string.Format("Processor count: {0}", Environment.ProcessorCount));
            builder.AppendLine(string.Format("Program data path: {0}", appPaths.ProgramDataPath));
            builder.AppendLine(string.Format("Application directory: {0}", appPaths.ProgramSystemPath));

            return builder;
        }

        protected abstract IJsonSerializer CreateJsonSerializer();

        private void SetHttpLimit()
        {
            try
            {
                // Increase the max http request limit
#if NET46
                ServicePointManager.DefaultConnectionLimit = Math.Max(96, ServicePointManager.DefaultConnectionLimit);
#endif    
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

            ConfigureAutorun();

            ConfigurationManager.ConfigurationUpdated += OnConfigurationUpdated;

            return Task.FromResult(true);
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
            ConfigurationManager.AddParts(GetExports<IConfigurationFactory>());
            Plugins = GetExports<IPlugin>().Select(LoadPlugin).Where(i => i != null).ToArray();
        }

        private IPlugin LoadPlugin(IPlugin plugin)
        {
            try
            {
                var assemblyPlugin = plugin as IPluginAssembly;

                if (assemblyPlugin != null)
                {
#if NET46
                    var assembly = plugin.GetType().Assembly;
                    var assemblyName = assembly.GetName();

                    var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
                    var assemblyId = new Guid(attribute.Value);

                    var assemblyFileName = assemblyName.Name + ".dll";
                    var assemblyFilePath = Path.Combine(ApplicationPaths.PluginsPath, assemblyFileName);

                    assemblyPlugin.SetAttributes(assemblyFilePath, assemblyFileName, assemblyName.Version, assemblyId);
#elif NETSTANDARD1_6
                    var typeInfo = plugin.GetType().GetTypeInfo();
                    var assembly = typeInfo.Assembly;
                    var assemblyName = assembly.GetName();

                    var attribute = (GuidAttribute)assembly.GetCustomAttribute(typeof(GuidAttribute));
                    var assemblyId = new Guid(attribute.Value);

                    var assemblyFileName = assemblyName.Name + ".dll";
                    var assemblyFilePath = Path.Combine(ApplicationPaths.PluginsPath, assemblyFileName);

                    assemblyPlugin.SetAttributes(assemblyFilePath, assemblyFileName, assemblyName.Version, assemblyId);
#else
return null;
#endif
                }

                var isFirstRun = !File.Exists(plugin.ConfigurationFilePath);
                plugin.SetStartupInfo(isFirstRun, File.GetLastWriteTimeUtc, s => Directory.CreateDirectory(s));
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error loading plugin {0}", ex, plugin.GetType().FullName);
                return null;
            }

            return plugin;
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
                .Where(t =>
                {
#if NET46
                    return t.IsClass && !t.IsAbstract && !t.IsInterface && !t.IsGenericType;
#endif    
#if NETSTANDARD1_6
                    var typeInfo = t.GetTypeInfo();
                    return typeInfo.IsClass && !typeInfo.IsAbstract && !typeInfo.IsInterface && !typeInfo.IsGenericType;
#endif
                    return false;
                })
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

            TaskManager = new TaskManager(ApplicationPaths, JsonSerializer, LogManager.GetLogger("TaskManager"), FileSystemManager, SystemEvents);

            RegisterSingleInstance(JsonSerializer);
            RegisterSingleInstance(XmlSerializer);
            RegisterSingleInstance(MemoryStreamFactory);
            RegisterSingleInstance(SystemEvents);

            RegisterSingleInstance(LogManager);
            RegisterSingleInstance(Logger);

            RegisterSingleInstance(TaskManager);
            RegisterSingleInstance(EnvironmentInfo);

            RegisterSingleInstance(FileSystemManager);

            HttpClient = new HttpClientManager.HttpClientManager(ApplicationPaths, LogManager.GetLogger("HttpClient"), FileSystemManager, MemoryStreamFactory);
            RegisterSingleInstance(HttpClient);

            RegisterSingleInstance(NetworkManager);

            IsoManager = new IsoManager();
            RegisterSingleInstance(IsoManager);

            ProcessFactory = new ProcessFactory();
            RegisterSingleInstance(ProcessFactory);

            TimerFactory = new TimerFactory();
            RegisterSingleInstance(TimerFactory);

            SocketFactory = new SocketFactory(LogManager.GetLogger("SocketFactory"));
            RegisterSingleInstance(SocketFactory);

            RegisterSingleInstance(CryptographyProvider);

            return Task.FromResult(true);
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
                Logger.ErrorException("Error creating {0}", ex, type.FullName);

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
                Logger.ErrorException("Error creating {0}", ex, type.FullName);
                // Don't blow up in release mode
                return null;
            }
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
#if NET46
                return Assembly.Load(File.ReadAllBytes(file));
#elif NETSTANDARD1_6
                
                return AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(File.ReadAllBytes(file)));
#endif
                return null;
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

#if NET46
            return AllConcreteTypes.Where(currentType.IsAssignableFrom);
#elif NETSTANDARD1_6
            var currentTypeInfo = currentType.GetTypeInfo();

            return AllConcreteTypes.Where(currentTypeInfo.IsAssignableFrom);
#endif
            return new List<Type>();
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
            Plugins = list.ToArray();
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
            Logger.Info("App needs to be restarted.");

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
