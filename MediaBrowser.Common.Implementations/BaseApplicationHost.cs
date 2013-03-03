using MediaBrowser.Common.Implementations.Udp;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Common.Implementations.WebSocket;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MediaBrowser.Common.Implementations
{
    public abstract class BaseApplicationHost
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get; protected set; }

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
        protected IApplicationPaths ApplicationPaths { get; private set; }

        /// <summary>
        /// The container
        /// </summary>
        protected readonly Container Container = new Container();
      
        /// <summary>
        /// Gets assemblies that failed to load
        /// </summary>
        public List<string> FailedAssemblies { get; protected set; }

        /// <summary>
        /// Gets all types within all running assemblies
        /// </summary>
        /// <value>All types.</value>
        public Type[] AllTypes { get; protected set; }

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
        private IProtobufSerializer _protobufSerializer;
        /// <summary>
        /// Gets the protobuf serializer.
        /// </summary>
        /// <value>The protobuf serializer.</value>
        protected IProtobufSerializer ProtobufSerializer
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _protobufSerializer, ref _protobufSerializerInitialized, ref _protobufSerializerSyncLock, () => Serialization.ProtobufSerializer.Create(AllTypes));
                return _protobufSerializer;
            }
            private set
            {
                _protobufSerializer = value;
                _protobufSerializerInitialized = value != null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationHost" /> class.
        /// </summary>
        protected BaseApplicationHost()
        {
            FailedAssemblies = new List<string>();

            ApplicationPaths = GetApplicationPaths();

            LogManager = GetLogManager();

            Logger = LogManager.GetLogger("App");

            IsFirstRun = !File.Exists(ApplicationPaths.SystemConfigurationFilePath);

            DiscoverTypes();
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected abstract IEnumerable<Assembly> GetComposablePartAssemblies();

        /// <summary>
        /// Gets the log manager.
        /// </summary>
        /// <returns>ILogManager.</returns>
        protected abstract ILogManager GetLogManager();

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <returns>IApplicationPaths.</returns>
        protected abstract IApplicationPaths GetApplicationPaths();

        /// <summary>
        /// Finds the parts.
        /// </summary>
        protected virtual void FindParts()
        {
            Resolve<IHttpServer>().Init(GetExports<IRestfulService>(false));
            Resolve<IServerManager>().AddWebSocketListeners(GetExports<IWebSocketListener>(false));

            Resolve<IServerManager>().Start();
            Resolve<ITaskManager>().AddTasks(GetExports<IScheduledTask>(false));

            Plugins = GetExports<IPlugin>();
        }
        
        /// <summary>
        /// Discovers the types.
        /// </summary>
        protected void DiscoverTypes()
        {
            FailedAssemblies.Clear();

            AllTypes = GetComposablePartAssemblies().SelectMany(GetTypes).ToArray();

            AllConcreteTypes = AllTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && !t.IsGenericType).ToArray();
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        protected virtual void RegisterResources(ITaskManager taskManager, INetworkManager networkManager, IServerManager serverManager)
        {
            RegisterSingleInstance(LogManager);
            RegisterSingleInstance(Logger);

            RegisterSingleInstance(ApplicationPaths);
            RegisterSingleInstance(taskManager);
            RegisterSingleInstance<IWebSocketServer>(() => new AlchemyServer(Logger));
            RegisterSingleInstance(ProtobufSerializer);
            RegisterSingleInstance<IUdpServer>(new UdpServer(Logger), false);
            RegisterSingleInstance<IPackageManager>(new PackageManager());
            RegisterSingleInstance<IHttpClient>(new HttpClientManager.HttpClientManager(ApplicationPaths, Logger));

            RegisterSingleInstance(networkManager);
            RegisterSingleInstance(serverManager);
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
        /// <param name="manageLifetime">if set to <c>true</c> [manage lifetime].</param>
        protected void RegisterSingleInstance<T>(T obj, bool manageLifetime = true)
            where T : class
        {
            Container.RegisterSingle(obj);

            if (manageLifetime)
            {
                var disposable = obj as IDisposable;

                if (disposable != null)
                {
                    Logger.Info("Registering " + disposable.GetType().Name);
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
            Container.RegisterSingle(func);
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
                return Assembly.Load(File.ReadAllBytes((file)));
            }
            catch (Exception ex)
            {
                FailedAssemblies.Add(file);
                Logger.ErrorException("Error loading assembly {0}", ex, file);
                return null;
            }
        }

        /// <summary>
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manageLiftime">if set to <c>true</c> [manage liftime].</param>
        /// <returns>IEnumerable{``0}.</returns>
        public IEnumerable<T> GetExports<T>(bool manageLiftime = true)
        {
            var currentType = typeof(T);

            Logger.Info("Composing instances of " + currentType.Name);

            var parts = AllConcreteTypes.Where(currentType.IsAssignableFrom).Select(CreateInstance).Cast<T>().ToArray();

            if (manageLiftime)
            {
                DisposableParts.AddRange(parts.OfType<IDisposable>());
            }

            return parts;
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
        /// Configures the auto run at startup.
        /// </summary>
        /// <param name="autorun">if set to <c>true</c> [autorun].</param>
        public void ConfigureAutoRunAtStartup(bool autorun)
        {
            
        }

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

                    part.Dispose();
                }
            }
        }
    }
}
