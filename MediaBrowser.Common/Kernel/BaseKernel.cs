using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Represents a shared base kernel for both the UI and server apps
    /// </summary>
    public abstract class BaseKernel<TConfigurationType>
        where TConfigurationType : BaseConfiguration, new()
    {
        /// <summary>
        /// Gets the path to the program data folder
        /// </summary>
        public string ProgramDataPath { get; private set; }

        /// <summary>
        /// Gets the path to the plugin directory
        /// </summary>
        protected string PluginsPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "plugins");
            }
        }

        /// <summary>
        /// Gets the path to the application configuration file
        /// </summary>
        protected string ConfigurationPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "config.js");
            }
        }

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public TConfigurationType Configuration { get; private set; }

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
            ProgramDataPath = GetProgramDataPath();

            Logger.LoggerInstance = new FileLogger(Path.Combine(ProgramDataPath, "Logs"));
        }

        public virtual void Init()
        {
            ReloadConfiguration();

            ReloadHttpServer();

            ReloadComposableParts();
        }

        /// <summary>
        /// Uses MEF to locate plugins
        /// Subclasses can use this to locate types within plugins
        /// </summary>
        protected void ReloadComposableParts()
        {
            if (!Directory.Exists(PluginsPath))
            {
                Directory.CreateDirectory(PluginsPath);
            }

            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            IEnumerable<Assembly> pluginAssemblies = Directory.GetFiles(PluginsPath, "*.dll", SearchOption.AllDirectories).Select(f => Assembly.Load(File.ReadAllBytes((f))));

            var catalog = new AggregateCatalog(pluginAssemblies.Select(a => new AssemblyCatalog(a)));
            //catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
            //catalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));

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
                plugin.Path = Path.Combine(PluginsPath, assemblyName.Name);

                plugin.ReloadConfiguration();

                if (plugin.Enabled)
                {
                    if (KernelContext == KernelContext.Server)
                    {
                        plugin.InitInServer();
                    }
                    else
                    {
                        plugin.InitInUI();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the path to the application's ProgramDataFolder
        /// </summary>
        private string GetProgramDataPath()
        {
            string programDataPath = ConfigurationManager.AppSettings["ProgramDataPath"];

            // If it's a relative path, e.g. "..\"
            if (!Path.IsPathRooted(programDataPath))
            {
                string path = Assembly.GetExecutingAssembly().Location;
                path = Path.GetDirectoryName(path);

                programDataPath = Path.Combine(path, programDataPath);

                programDataPath = Path.GetFullPath(programDataPath);
            }

            if (!Directory.Exists(programDataPath))
            {
                Directory.CreateDirectory(programDataPath);
            }

            return programDataPath;
        }

        /// <summary>
        /// Reloads application configuration from the config file
        /// </summary>
        private void ReloadConfiguration()
        {
            // Deserialize config
            if (!File.Exists(ConfigurationPath))
            {
                Configuration = new TConfigurationType();
            }
            else
            {
                Configuration = JsonSerializer.DeserializeFromFile<TConfigurationType>(ConfigurationPath);
            }

            Logger.LoggerInstance.LogSeverity = Configuration.LogSeverity;
        }

        /// <summary>
        /// Saves the current application configuration to the config file
        /// </summary>
        public void SaveConfiguration()
        {
            JsonSerializer.SerializeToFile(Configuration, ConfigurationPath);
        }

        /// <summary>
        /// Restarts the Http Server, or starts it if not currently running
        /// </summary>
        private void ReloadHttpServer()
        {
            if (HttpServer != null)
            {
                HttpServer.Dispose();
            }

            HttpServer = new HttpServer("http://+:" + Configuration.HttpServerPortNumber + "/mediabrowser/");
        }

        /// <summary>
        /// This snippet will allow any plugin to reference another
        /// </summary>
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);

            // Look for the .dll recursively within the plugins directory
            string dll = Directory.GetFiles(PluginsPath, "*.dll", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == assemblyName.Name);

            // If we found a matching assembly, load it now
            if (!string.IsNullOrEmpty(dll))
            {
                return Assembly.Load(File.ReadAllBytes(dll));
            }

            return null;
        }
    }
}
