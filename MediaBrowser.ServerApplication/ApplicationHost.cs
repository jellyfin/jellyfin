using BDInfo;
using MediaBrowser.ClickOnce;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.Implementations.Serialization;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.IsoMounter;
using MediaBrowser.Logging.Nlog;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Networking.HttpManager;
using MediaBrowser.Networking.HttpServer;
using MediaBrowser.Networking.Management;
using MediaBrowser.Networking.Udp;
using MediaBrowser.Networking.WebSocket;
using MediaBrowser.Server.Implementations;
using MediaBrowser.ServerApplication.Implementations;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Class CompositionRoot
    /// </summary>
    public class ApplicationHost : IApplicationHost, IDisposable
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        public string LogFilePath { get; private set; }

        /// <summary>
        /// The container
        /// </summary>
        private readonly Container _container = new Container();

        /// <summary>
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        public Kernel Kernel { get; private set; }

        private readonly List<string> _failedAssemblies = new List<string>();
        /// <summary>
        /// Gets assemblies that failed to load
        /// </summary>
        public IEnumerable<string> FailedAssemblies
        {
            get { return _failedAssemblies; }
        }

        /// <summary>
        /// Gets all types within all running assemblies
        /// </summary>
        /// <value>All types.</value>
        public Type[] AllTypes { get; private set; }

        /// <summary>
        /// Gets all concrete types.
        /// </summary>
        /// <value>All concrete types.</value>
        public Type[] AllConcreteTypes { get; private set; }

        /// <summary>
        /// The disposable parts
        /// </summary>
        private readonly List<IDisposable> _disposableParts = new List<IDisposable>();

        /// <summary>
        /// The json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

        /// <summary>
        /// The _XML serializer
        /// </summary>
        private readonly IXmlSerializer _xmlSerializer = new XmlSerializer();

        /// <summary>
        /// The _application paths
        /// </summary>
        private readonly IServerApplicationPaths _applicationPaths = new ServerApplicationPaths();

        /// <summary>
        /// The _task manager
        /// </summary>
        private readonly ITaskManager _taskManager;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ApplicationHost(ILogger logger)
        {
            Logger = logger;

            _taskManager = new TaskManager(_applicationPaths, _jsonSerializer, Logger);

            Kernel = new Kernel(this, _applicationPaths, _xmlSerializer, _taskManager, Logger);
            ReloadLogger();

            RegisterResources();

            FindParts();
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        internal void RegisterResources()
        {
            DiscoverTypes();

            RegisterSingleInstance<IKernel>(Kernel);
            RegisterSingleInstance(Kernel);
            
            RegisterSingleInstance<IApplicationHost>(this);
            RegisterSingleInstance(Logger);

            RegisterSingleInstance(_applicationPaths);
            RegisterSingleInstance<IApplicationPaths>(_applicationPaths);
            RegisterSingleInstance(_taskManager);
            RegisterSingleInstance<IIsoManager>(() => new PismoIsoManager(Logger));
            RegisterSingleInstance<IBlurayExaminer>(() => new BdInfoExaminer());
            RegisterSingleInstance<IHttpClient>(() => new HttpManager(_applicationPaths, Logger));
            RegisterSingleInstance<INetworkManager>(() => new NetworkManager());
            RegisterSingleInstance<IZipClient>(() => new DotNetZipClient());
            RegisterSingleInstance<IWebSocketServer>(() => new AlchemyServer(Logger));
            RegisterSingleInstance(_jsonSerializer);
            RegisterSingleInstance(_xmlSerializer);
            RegisterSingleInstance<IProtobufSerializer>(() => ProtobufSerializer);
            Register(typeof(IUdpServer), typeof(UdpServer));
            RegisterSingleInstance(() => ServerFactory.CreateServer(this, Kernel, ProtobufSerializer, Logger, "Media Browser", "index.html"));
        }

        /// <summary>
        /// Discovers the types.
        /// </summary>
        private void DiscoverTypes()
        {
            _failedAssemblies.Clear();

            AllTypes = GetComposablePartAssemblies().SelectMany(GetTypes).ToArray();

            AllConcreteTypes = AllTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && !t.IsGenericType).ToArray();
        }

        /// <summary>
        /// Finds the parts.
        /// </summary>
        private void FindParts()
        {
            _taskManager.AddTasks(GetExports<IScheduledTask>(false));
        }

        /// <summary>
        /// Gets a list of types within an assembly
        /// This will handle situations that would normally throw an exception - such as a type within the assembly that depends on some other non-existant reference
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>IEnumerable{Type}.</returns>
        /// <exception cref="System.ArgumentNullException">assembly</exception>
        private IEnumerable<Type> GetTypes(Assembly assembly)
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
        private ProtobufSerializer _protobufSerializer;
        /// <summary>
        /// Gets the protobuf serializer.
        /// </summary>
        /// <value>The protobuf serializer.</value>
        public ProtobufSerializer ProtobufSerializer
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _protobufSerializer, ref _protobufSerializerInitialized, ref _protobufSerializerSyncLock, () => ProtobufSerializer.Create(AllTypes));
                return _protobufSerializer;
            }
            private set
            {
                _protobufSerializer = value;
                _protobufSerializerInitialized = value != null;
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
                return _container.GetInstance(type);
            }
            catch
            {
                Logger.Error("Error creating {0}", type.Name);

                throw;
            }
        }

        /// <summary>
        /// Registers the specified obj.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        public void RegisterSingleInstance<T>(T obj)
            where T : class
        {
            _container.RegisterSingle(obj);
        }

        /// <summary>
        /// Registers the specified func.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">The func.</param>
        public void Register<T>(Func<T> func)
            where T : class
        {
            _container.Register(func);
        }

        /// <summary>
        /// Registers the single instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">The func.</param>
        public void RegisterSingleInstance<T>(Func<T> func)
            where T : class
        {
            _container.RegisterSingle(func);
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T Resolve<T>()
        {
            return (T)_container.GetRegistration(typeof(T), true).GetInstance();
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T TryResolve<T>()
        {
            var result = _container.GetRegistration(typeof(T), false);

            if (result == null)
            {
                return default(T);
            }
            return (T)result.GetInstance();
        }

        /// <summary>
        /// Registers the specified service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="implementation">Type of the concrete.</param>
        public void Register(Type serviceType, Type implementation)
        {
            _container.Register(serviceType, implementation);
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Restart()
        {
            App.Instance.Restart();
        }

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ReloadLogger()
        {
            LogFilePath = Path.Combine(_applicationPaths.LogDirectoryPath, "Server-" + DateTime.Now.Ticks + ".log");

            NlogManager.AddFileTarget(LogFilePath, Kernel.Configuration.EnableDebugLevelLogging);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public bool CanSelfUpdate
        {
            get { return ClickOnceHelper.IsNetworkDeployed; }
        }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new ApplicationUpdateCheck().CheckForApplicationUpdate(cancellationToken, progress);
        }

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task UpdateApplication(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new ApplicationUpdater().UpdateApplication(cancellationToken, progress);
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        private IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            foreach (var pluginAssembly in Directory
                .EnumerateFiles(Kernel.ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(LoadAssembly).Where(a => a != null))
            {
                yield return pluginAssembly;
            }

            var runningDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            var corePluginDirectory = Path.Combine(runningDirectory, "CorePlugins");

            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            foreach (var pluginAssembly in Directory
                .EnumerateFiles(corePluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(LoadAssembly).Where(a => a != null))
            {
                yield return pluginAssembly;
            }

            // Include composable parts in the Model assembly 
            yield return typeof(SystemInfo).Assembly;

            // Include composable parts in the Common assembly 
            yield return typeof(IKernel).Assembly;

            // Include composable parts in the Controller assembly 
            yield return typeof(Kernel).Assembly;

            // Common implementations
            yield return typeof(TaskManager).Assembly;

            // Server implementations
            yield return typeof(ServerApplicationPaths).Assembly;
            
            // Include composable parts in the running assembly
            yield return GetType().Assembly;
        }

        /// <summary>
        /// Loads the assembly.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>Assembly.</returns>
        private Assembly LoadAssembly(string file)
        {
            try
            {
                return Assembly.Load(File.ReadAllBytes((file)));
            }
            catch (Exception ex)
            {
                _failedAssemblies.Add(file);
                Logger.ErrorException("Error loading assembly {0}", ex, file);
                return null;
            }
        }

        /// <summary>
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="allTypes">All types.</param>
        /// <param name="manageLiftime">if set to <c>true</c> [manage liftime].</param>
        /// <returns>IEnumerable{``0}.</returns>
        public IEnumerable<T> GetExports<T>(bool manageLiftime = true)
        {
            var currentType = typeof(T);

            Logger.Info("Composing instances of " + currentType.Name);

            var parts = AllConcreteTypes.Where(currentType.IsAssignableFrom).Select(CreateInstance).Cast<T>().ToArray();

            if (manageLiftime)
            {
                _disposableParts.AddRange(parts.OfType<IDisposable>());
            }

            return parts;
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
            foreach (var part in _disposableParts)
            {
                part.Dispose();
            }

            _disposableParts.Clear();
        }
    }
}
