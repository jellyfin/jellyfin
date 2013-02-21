using MediaBrowser.Common.Events;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Mef;
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
using System.ComponentModel.Composition.Primitives;
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
        #region ReloadBeginning Event
        /// <summary>
        /// Fires whenever the kernel begins reloading
        /// </summary>
        public event EventHandler<GenericEventArgs<IProgress<TaskProgress>>> ReloadBeginning;
        private void OnReloadBeginning(IProgress<TaskProgress> progress)
        {
            if (ReloadBeginning != null)
            {
                ReloadBeginning(this, new GenericEventArgs<IProgress<TaskProgress>> { Argument = progress });
            }
        }
        #endregion

        #region ReloadCompleted Event
        /// <summary>
        /// Fires whenever the kernel completes reloading
        /// </summary>
        public event EventHandler<GenericEventArgs<IProgress<TaskProgress>>> ReloadCompleted;
        private void OnReloadCompleted(IProgress<TaskProgress> progress)
        {
            if (ReloadCompleted != null)
            {
                ReloadCompleted(this, new GenericEventArgs<IProgress<TaskProgress>> { Argument = progress });
            }
        }
        #endregion

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
        /// Gets the list of currently registered Loggers
        /// </summary>
        [ImportMany(typeof(BaseLogger))]
        public IEnumerable<BaseLogger> Loggers { get; set; }

        /// <summary>
        /// Both the Ui and server will have a built-in HttpServer.
        /// People will inevitably want remote control apps so it's needed in the Ui too.
        /// </summary>
        public HttpServer HttpServer { get; private set; }

        /// <summary>
        /// This subscribes to HttpListener requests and finds the appropate BaseHandler to process it
        /// </summary>
        private IDisposable HttpListener { get; set; }

        /// <summary>
        /// Gets the MEF CompositionContainer
        /// </summary>
        private CompositionContainer CompositionContainer { get; set; }

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
            Logger.Kernel = this;

            // Performs initializations that only occur once
            InitializeInternal(progress);

            // Performs initializations that can be reloaded at anytime
            await Reload(progress).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs initializations that only occur once
        /// </summary>
        protected virtual void InitializeInternal(IProgress<TaskProgress> progress)
        {
            ApplicationPaths = new TApplicationPathsType();

            ReportProgress(progress, "Loading Configuration");
            ReloadConfiguration();

            ReportProgress(progress, "Loading Http Server");
            ReloadHttpServer();
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        public async Task Reload(IProgress<TaskProgress> progress)
        {
            OnReloadBeginning(progress);

            await ReloadInternal(progress).ConfigureAwait(false);

            OnReloadCompleted(progress);

            ReportProgress(progress, "Kernel.Reload Complete");
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        protected virtual async Task ReloadInternal(IProgress<TaskProgress> progress)
        {
            await Task.Run(() =>
            {
                ReportProgress(progress, "Loading Plugins");
                ReloadComposableParts();

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Uses MEF to locate plugins
        /// Subclasses can use this to locate types within plugins
        /// </summary>
        private void ReloadComposableParts()
        {
            DisposeComposableParts();

            CompositionContainer = GetCompositionContainer(includeCurrentAssembly: true);

            CompositionContainer.ComposeParts(this);

            OnComposablePartsLoaded();

            CompositionContainer.Catalog.Dispose();
        }

        /// <summary>
        /// Constructs an MEF CompositionContainer based on the current running assembly and all plugin assemblies
        /// </summary>
        public CompositionContainer GetCompositionContainer(bool includeCurrentAssembly = false)
        {
            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            IEnumerable<Assembly> pluginAssemblies = Directory.GetFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly).Select(f => Assembly.Load(File.ReadAllBytes((f))));

            var catalogs = new List<ComposablePartCatalog>();

            catalogs.AddRange(pluginAssemblies.Select(a => new AssemblyCatalog(a)));

            // Include composable parts in the Common assembly 
            catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));

            if (includeCurrentAssembly)
            {
                // Include composable parts in the subclass assembly
                catalogs.Add(new AssemblyCatalog(GetType().Assembly));
            }

            return MefUtils.GetSafeCompositionContainer(catalogs);
        }

        /// <summary>
        /// Fires after MEF finishes finding composable parts within plugin assemblies
        /// </summary>
        protected virtual void OnComposablePartsLoaded()
        {
            foreach (var logger in Loggers)
            {
                logger.Initialize(this);
            }

            // Start-up each plugin
            foreach (var plugin in Plugins)
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
            // Use try/catch to avoid the extra file system lookup using File.Exists
            try
            {
                Configuration = XmlSerializer.DeserializeFromFile<TConfigurationType>(ApplicationPaths.SystemConfigurationFilePath);
            }
            catch (FileNotFoundException)
            {
                Configuration = new TConfigurationType();
                XmlSerializer.SerializeToFile(Configuration, ApplicationPaths.SystemConfigurationFilePath);
            }
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

            DisposeHttpServer();

            DisposeComposableParts();
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
        /// Gets the current application version
        /// </summary>
        public Version ApplicationVersion
        {
            get
            {
                return GetType().Assembly.GetName().Version;
            }
        }

        protected void ReportProgress(IProgress<TaskProgress> progress, string message)
        {
            progress.Report(new TaskProgress { Description = message });

            Logger.LogInfo(message);
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
        IEnumerable<BaseLogger> Loggers { get; }
        void Dispose();
    }
}
