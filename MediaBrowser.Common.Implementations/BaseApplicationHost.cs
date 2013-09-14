using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Implementations.NetworkManagement;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.Implementations.Security;
using MediaBrowser.Common.Implementations.Serialization;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations
{
    /// <summary>
    /// Class BaseApplicationHost
    /// </summary>
    /// <typeparam name="TApplicationPathsType">The type of the T application paths type.</typeparam>
    public abstract class BaseApplicationHost<TApplicationPathsType> : IApplicationHost
        where TApplicationPathsType : class, IApplicationPaths, new()
    {
        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        public event EventHandler HasPendingRestartChanged;

        /// <summary>
        /// Occurs when [application updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<Version>> ApplicationUpdated;

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
        protected TApplicationPathsType ApplicationPaths = new TApplicationPathsType();

        /// <summary>
        /// The container
        /// </summary>
        protected readonly Container Container = new Container();

        /// <summary>
        /// The json serializer
        /// </summary>
        public readonly IJsonSerializer JsonSerializer = new JsonSerializer();

        /// <summary>
        /// The _XML serializer
        /// </summary>
        protected readonly IXmlSerializer XmlSerializer = new XmlSerializer();

        /// <summary>
        /// Gets assemblies that failed to load
        /// </summary>
        /// <value>The failed assemblies.</value>
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
        protected IHttpClient HttpClient { get; private set; }
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
        protected IInstallationManager InstallationManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationHost{TApplicationPathsType}"/> class.
        /// </summary>
        protected BaseApplicationHost()
        {
            FailedAssemblies = new List<string>();

            LogManager = new NlogManager(ApplicationPaths.LogDirectoryPath, LogFilePrefixName);

            ConfigurationManager = GetConfigurationManager();
        }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public virtual async Task Init()
        {
            IsFirstRun = !ConfigurationManager.CommonConfiguration.IsStartupWizardCompleted;

            Logger = LogManager.GetLogger("App");

            LogManager.ReloadLogger(ConfigurationManager.CommonConfiguration.EnableDebugLevelLogging ? LogSeverity.Debug : LogSeverity.Info);
            OnLoggerLoaded();

            DiscoverTypes();

            Logger.Info("Version {0} initializing", ApplicationVersion);

            await RegisterResources().ConfigureAwait(false);

            FindParts();
        }

        protected virtual void OnLoggerLoaded()
        {

        }

        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        /// <returns>Task.</returns>
        public virtual Task RunStartupTasks()
        {
            return Task.Run(() =>
            {
                Resolve<ITaskManager>().AddTasks(GetExports<IScheduledTask>(false));

                Task.Run(() => ConfigureAutoRunAtStartup());

                ConfigurationManager.ConfigurationUpdated += OnConfigurationUpdated;
            });
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected abstract IEnumerable<Assembly> GetComposablePartAssemblies();

        /// <summary>
        /// Gets the name of the log file prefix.
        /// </summary>
        /// <value>The name of the log file prefix.</value>
        protected abstract string LogFilePrefixName { get; }

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
            Plugins = GetExports<IPlugin>();
        }

        /// <summary>
        /// Discovers the types.
        /// </summary>
        protected void DiscoverTypes()
        {
            FailedAssemblies.Clear();

            var assemblies = GetComposablePartAssemblies().ToArray();

            foreach (var assembly in assemblies)
            {
                Logger.Info("Loading {0}", assembly.FullName);
            }

            AllTypes = assemblies.SelectMany(GetTypes).ToArray();

            AllConcreteTypes = AllTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && !t.IsGenericType).ToArray();
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task RegisterResources()
        {
            return Task.Run(() =>
            {
                RegisterSingleInstance(ConfigurationManager);
                RegisterSingleInstance<IApplicationHost>(this);

                RegisterSingleInstance<IApplicationPaths>(ApplicationPaths);

                TaskManager = new TaskManager(ApplicationPaths, JsonSerializer, Logger);

                RegisterSingleInstance(JsonSerializer);
                RegisterSingleInstance(XmlSerializer);

                RegisterSingleInstance(LogManager);
                RegisterSingleInstance(Logger);

                RegisterSingleInstance(TaskManager);

                HttpClient = new HttpClientManager.HttpClientManager(ApplicationPaths, Logger, GetHttpMessageHandler);
                RegisterSingleInstance(HttpClient);

                NetworkManager = new NetworkManager();
                RegisterSingleInstance(NetworkManager);

                SecurityManager = new PluginSecurityManager(this, HttpClient, JsonSerializer, ApplicationPaths, NetworkManager);
                RegisterSingleInstance(SecurityManager);

                InstallationManager = new InstallationManager(Logger, this, ApplicationPaths, HttpClient, JsonSerializer, SecurityManager, NetworkManager, ConfigurationManager);
                RegisterSingleInstance(InstallationManager);
            });
        }

        protected abstract HttpMessageHandler GetHttpMessageHandler(bool enableHttpCompression);

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
            catch (Exception ex)
            {
                Logger.Error("Error creating {0}", ex, type.Name);

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
            var parts = GetExportTypes<T>().Select(CreateInstance).Cast<T>().ToArray();

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
        /// Defines the full path to our shortcut in the start menu
        /// </summary>
        protected abstract string ProductShortcutPath { get; }

        /// <summary>
        /// Handles the ConfigurationUpdated event of the ConfigurationManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual void OnConfigurationUpdated(object sender, EventArgs e)
        {
            ConfigureAutoRunAtStartup();
        }

        /// <summary>
        /// Configures the auto run at startup.
        /// </summary>
        private void ConfigureAutoRunAtStartup()
        {
            if (ConfigurationManager.CommonConfiguration.RunAtStartup)
            {
                //Copy our shortut into the startup folder for this user
                File.Copy(ProductShortcutPath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Path.GetFileName(ProductShortcutPath) ?? "MBstartup.lnk"), true);
            }
            else
            {
                //Remove our shortcut from the startup folder for this user
                try
                {
                    File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Path.GetFileName(ProductShortcutPath) ?? "MBstartup.lnk"));
                }
                catch (FileNotFoundException)
                {
                    //This is okay - trying to remove it anyway
                }
            }
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

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public abstract void Restart();

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
        public abstract void Shutdown();

        /// <summary>
        /// Called when [application updated].
        /// </summary>
        /// <param name="newVersion">The new version.</param>
        protected void OnApplicationUpdated(Version newVersion)
        {
            EventHelper.QueueEventIfNotNull(ApplicationUpdated, this, new GenericEventArgs<Version> { Argument = newVersion }, Logger);

            NotifyPendingRestart();
        }
    }
}
