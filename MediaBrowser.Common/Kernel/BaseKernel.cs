using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Progress;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Represents a shared base kernel for both the Ui and server apps
    /// </summary>
    public abstract class BaseKernel<TConfigurationType, TApplicationPathsType> : IDisposable, IKernel
        where TConfigurationType : BaseApplicationConfiguration, new()
        where TApplicationPathsType : BaseApplicationPaths, new()
    {
        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public TConfigurationType Configuration { get; private set; }

        public TApplicationPathsType ApplicationPaths { get; private set; }

        /// <summary>
        /// Gets the list of currently loaded plugins
        /// </summary>
        [ImportMany(typeof(BasePlugin))]
        public IEnumerable<BasePlugin> Plugins { get; private set; }

        /// <summary>
        /// Gets the list of currently registered http handlers
        /// </summary>
        [ImportMany(typeof(BaseHandler))]
        private IEnumerable<BaseHandler> HttpHandlers { get; set; }

        /// <summary>
        /// Both the Ui and server will have a built-in HttpServer.
        /// People will inevitably want remote control apps so it's needed in the Ui too.
        /// </summary>
        public HttpServer HttpServer { get; private set; }

        /// <summary>
        /// This subscribes to HttpListener requests and finds the appropate BaseHandler to process it
        /// </summary>
        private IDisposable HttpListener { get; set; }

        protected virtual string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + Configuration.HttpServerPortNumber + "/mediabrowser/";
            }
        }

        /// <summary>
        /// Gets the kernel context. Subclasses will have to override.
        /// </summary>
        public abstract KernelContext KernelContext { get; }

        /// <summary>
        /// Initializes the Kernel
        /// </summary>
        public async Task Init(IProgress<TaskProgress> progress)
        {
            // Performs initializations that only occur once
            InitializeInternal(progress);

            // Performs initializations that can be reloaded at anytime
            await Reload(progress).ConfigureAwait(false);

            progress.Report(new TaskProgress { Description = "Loading Complete", PercentComplete = 100 });
        }

        /// <summary>
        /// Performs initializations that only occur once
        /// </summary>
        protected virtual void InitializeInternal(IProgress<TaskProgress> progress)
        {
            ApplicationPaths = new TApplicationPathsType();
            
            ReloadLogger();

            progress.Report(new TaskProgress { Description = "Loading configuration", PercentComplete = 0 });
            ReloadConfiguration();

            progress.Report(new TaskProgress { Description = "Starting Http server", PercentComplete = 5 });
            ReloadHttpServer();
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        public virtual async Task Reload(IProgress<TaskProgress> progress)
        {
            await Task.Run(() =>
            {
                progress.Report(new TaskProgress { Description = "Loading Plugins", PercentComplete = 10 });
                ReloadComposableParts();

            }).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Disposes the current logger and creates a new one
        /// </summary>
        private void ReloadLogger()
        {
            DisposeLogger();

            DateTime now = DateTime.Now;

            string logFilePath = Path.Combine(ApplicationPaths.LogDirectoryPath, "log-" + now.ToString("dMyyyy") + "-" + now.Ticks + ".log");

            Trace.Listeners.Add(new TextWriterTraceListener(logFilePath));
            Trace.AutoFlush = true;

            Logger.LoggerInstance = new TraceLogger();
        }

        /// <summary>
        /// Uses MEF to locate plugins
        /// Subclasses can use this to locate types within plugins
        /// </summary>
        private void ReloadComposableParts()
        {
            DisposeComposableParts();

            var container = GetCompositionContainer(includeCurrentAssembly: true);

            container.ComposeParts(this);

            OnComposablePartsLoaded();

            container.Catalog.Dispose();
            container.Dispose();
        }

        /// <summary>
        /// Constructs an MEF CompositionContainer based on the current running assembly and all plugin assemblies
        /// </summary>
        public CompositionContainer GetCompositionContainer(bool includeCurrentAssembly = false)
        {
            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            IEnumerable<Assembly> pluginAssemblies = Directory.GetFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly).Select(f => Assembly.Load(File.ReadAllBytes((f))));

            var catalog = new AggregateCatalog(pluginAssemblies.Select(a => new AssemblyCatalog(a)));

            // Include composable parts in the Common assembly 
            // Uncomment this if it's ever needed
            //catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));

            if (includeCurrentAssembly)
            {
                // Include composable parts in the subclass assembly
                catalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));
            }

            return new CompositionContainer(catalog);
        }

        /// <summary>
        /// Fires after MEF finishes finding composable parts within plugin assemblies
        /// </summary>
        protected virtual void OnComposablePartsLoaded()
        {
            // Start-up each plugin
            foreach (BasePlugin plugin in Plugins)
            {
                plugin.Initialize(this);
            }
        }

        /// <summary>
        /// Reloads application configuration from the config file
        /// </summary>
        private void ReloadConfiguration()
        {
            //Configuration information for anything other than server-specific configuration will have to come via the API... -ebr

            // Deserialize config
            if (!File.Exists(ApplicationPaths.SystemConfigurationFilePath))
            {
                Configuration = new TConfigurationType();
                XmlSerializer.SerializeToFile(Configuration, ApplicationPaths.SystemConfigurationFilePath);
            }
            else
            {
                Configuration = XmlSerializer.DeserializeFromFile<TConfigurationType>(ApplicationPaths.SystemConfigurationFilePath);
            }

            Logger.LoggerInstance.LogSeverity = Configuration.EnableDebugLevelLogging ? LogSeverity.Debug : LogSeverity.Info;
        }

        /// <summary>
        /// Restarts the Http Server, or starts it if not currently running
        /// </summary>
        private void ReloadHttpServer()
        {
            DisposeHttpServer();

            HttpServer = new HttpServer(HttpServerUrlPrefix);

            HttpListener = HttpServer.Subscribe(ctx =>
            {
                BaseHandler handler = HttpHandlers.FirstOrDefault(h => h.HandlesRequest(ctx.Request));

                // Find the appropiate http handler
                if (handler != null)
                {
                    // Need to create a new instance because handlers are currently stateful
                    handler = Activator.CreateInstance(handler.GetType()) as BaseHandler;

                    // No need to await this, despite the compiler warning
                    handler.ProcessRequest(ctx);
                }
            });
        }

        /// <summary>
        /// Disposes all resources currently in use.
        /// </summary>
        public virtual void Dispose()
        {
            Logger.LogInfo("Beginning Kernel.Dispose");
            
            DisposeComposableParts();

            DisposeHttpServer();

            DisposeLogger();
        }

        /// <summary>
        /// Disposes all objects gathered through MEF composable parts
        /// </summary>
        protected virtual void DisposeComposableParts()
        {
            DisposePlugins();
        }

        /// <summary>
        /// Disposes all plugins
        /// </summary>
        private void DisposePlugins()
        {
            if (Plugins != null)
            {
                Logger.LogInfo("Disposing Plugins");
                
                foreach (BasePlugin plugin in Plugins)
                {
                    plugin.Dispose();
                }
            }
        }

        /// <summary>
        /// Disposes the current HttpServer
        /// </summary>
        private void DisposeHttpServer()
        {
            if (HttpServer != null)
            {
                Logger.LogInfo("Disposing Http Server");
                
                HttpServer.Dispose();
            }

            if (HttpListener != null)
            {
                HttpListener.Dispose();
            }
        }

        /// <summary>
        /// Disposes the current Logger instance
        /// </summary>
        private void DisposeLogger()
        {
            Trace.Listeners.Clear();

            if (Logger.LoggerInstance != null)
            {
                Logger.LogInfo("Disposing Logger");
                
                Logger.LoggerInstance.Dispose();
            }
        }

        /// <summary>
        /// Gets the current application version
        /// </summary>
        public Version ApplicationVersion
        {
            get
            {
                return GetType().Assembly.GetName().Version;
            }
        }

        BaseApplicationPaths IKernel.ApplicationPaths
        {
            get { return ApplicationPaths; }
        }
    }

    public interface IKernel
    {
        BaseApplicationPaths ApplicationPaths { get; }
        KernelContext KernelContext { get; }

        Task Init(IProgress<TaskProgress> progress);
        Task Reload(IProgress<TaskProgress> progress);
        void Dispose();
    }
}
