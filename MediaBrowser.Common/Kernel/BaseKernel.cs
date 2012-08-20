using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Progress;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Represents a shared base kernel for both the UI and server apps
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
        /// Both the UI and server will have a built-in HttpServer.
        /// People will inevitably want remote control apps so it's needed in the UI too.
        /// </summary>
        public HttpServer HttpServer { get; private set; }

        /// <summary>
        /// Gets the kernel context. The UI kernel will have to override this.
        /// </summary>
        protected KernelContext KernelContext { get { return KernelContext.Server; } }

        public BaseKernel()
        {
            ApplicationPaths = new TApplicationPathsType();
        }

        public virtual Task Init(IProgress<TaskProgress> progress)
        {
            return Task.Run(() =>
            {
                ReloadLogger();

                progress.Report(new TaskProgress() { Description = "Loading configuration", PercentComplete = 0 });
                ReloadConfiguration();

                progress.Report(new TaskProgress() { Description = "Starting Http server", PercentComplete = 5 });
                ReloadHttpServer();

                progress.Report(new TaskProgress() { Description = "Loading Plugins", PercentComplete = 10 });
                ReloadComposableParts();
            });
        }

        /// <summary>
        /// Gets or sets the path to the current log file
        /// </summary>
        public static string LogFilePath { get; set; }

        private void ReloadLogger()
        {
            DisposeLogger();

            DateTime now = DateTime.Now;

            LogFilePath = Path.Combine(ApplicationPaths.LogDirectoryPath, Assembly.GetExecutingAssembly().GetType().Name + "-" + now.ToString("dMyyyy") + "-" + now.Ticks + ".log");

            FileStream fs = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);

            Logger.LoggerInstance = new StreamLogger(fs);
        }

        /// <summary>
        /// Uses MEF to locate plugins
        /// Subclasses can use this to locate types within plugins
        /// </summary>
        protected void ReloadComposableParts()
        {
            DisposeComposableParts();
            
            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            IEnumerable<Assembly> pluginAssemblies = Directory.GetFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.AllDirectories).Select(f => Assembly.Load(File.ReadAllBytes((f))));

            var catalog = new AggregateCatalog(pluginAssemblies.Select(a => new AssemblyCatalog(a)));
            
            // Include composable parts in the Common assembly 
            // Uncomment this if it's ever needed
            //catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
            
            // Include composable parts in the subclass assembly
            catalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));

            var container = new CompositionContainer(catalog);

            container.ComposeParts(this);

            OnComposablePartsLoaded();

            catalog.Dispose();
            container.Dispose();
        }

        /// <summary>
        /// Fires after MEF finishes finding composable parts within plugin assemblies
        /// </summary>
        protected virtual void OnComposablePartsLoaded()
        {
            // This event handler will allow any plugin to reference another
            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            StartPlugins();
        }

        /// <summary>
        /// Initializes all plugins
        /// </summary>
        private void StartPlugins()
        {
            foreach (BasePlugin plugin in Plugins)
            {
                Assembly assembly = plugin.GetType().Assembly;
                AssemblyName assemblyName = assembly.GetName();

                plugin.Version = assemblyName.Version;
                plugin.Path = Path.Combine(ApplicationPaths.PluginsPath, assemblyName.Name);

                plugin.Context = KernelContext;

                plugin.ReloadConfiguration();

                if (plugin.Enabled)
                {
                    plugin.Init();
                }
            }
        }


        /// <summary>
        /// Reloads application configuration from the config file
        /// </summary>
        protected virtual void ReloadConfiguration()
        {
            //Configuration information for anything other than server-specific configuration will have to come via the API... -ebr

            // Deserialize config
            if (!File.Exists(ApplicationPaths.SystemConfigurationFilePath))
            {
                Configuration = new TConfigurationType();
            }
            else
            {
                Configuration = XmlSerializer.DeserializeFromFile<TConfigurationType>(ApplicationPaths.SystemConfigurationFilePath);
            }

            Logger.LoggerInstance.LogSeverity = Configuration.LogSeverity;
        }

        /// <summary>
        /// Restarts the Http Server, or starts it if not currently running
        /// </summary>
        private void ReloadHttpServer()
        {
            DisposeHttpServer();

            HttpServer = new HttpServer("http://+:" + Configuration.HttpServerPortNumber + "/mediabrowser/");
        }

        /// <summary>
        /// This snippet will allow any plugin to reference another
        /// </summary>
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);

            // Look for the .dll recursively within the plugins directory
            string dll = Directory.GetFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == assemblyName.Name);

            // If we found a matching assembly, load it now
            if (!string.IsNullOrEmpty(dll))
            {
                return Assembly.Load(File.ReadAllBytes(dll));
            }

            return null;
        }

        /// <summary>
        /// Disposes all resources currently in use.
        /// </summary>
        public virtual void Dispose()
        {
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
                HttpServer.Dispose();
            }
        }

        /// <summary>
        /// Disposes the current Logger instance
        /// </summary>
        private void DisposeLogger()
        {
            if (Logger.LoggerInstance != null)
            {
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
    }

    public interface IKernel
    {
        Task Init(IProgress<TaskProgress> progress);
        void Dispose();
    }
}
