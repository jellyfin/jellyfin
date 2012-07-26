using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

        protected string PluginsPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "plugins");
            }
        }

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

        protected void ReloadComposableParts()
        {
            if (!Directory.Exists(PluginsPath))
            {
                Directory.CreateDirectory(PluginsPath);
            }
            
            var catalog = new AggregateCatalog(Directory.GetDirectories(PluginsPath, "*", SearchOption.TopDirectoryOnly).Select(f => new DirectoryCatalog(f)));

            //catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
            //catalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));

            new CompositionContainer(catalog).ComposeParts(this);

            OnComposablePartsLoaded();
        }

        protected virtual void OnComposablePartsLoaded()
        {
            StartPlugins();
        }

        private void StartPlugins()
        {
            Parallel.For(0, Plugins.Count(), i =>
            {
                var plugin = Plugins.ElementAt(i);

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
            });
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

        public void SaveConfiguration()
        {
            JsonSerializer.SerializeToFile(Configuration, ConfigurationPath);
        }

        private void ReloadHttpServer()
        {
            if (HttpServer != null)
            {
                HttpServer.Dispose();
            }

            HttpServer = new HttpServer("http://+:" + Configuration.HttpServerPortNumber + "/mediabrowser/");
        }

        private static TConfigurationType GetConfiguration(string directory)
        {
            string file = Path.Combine(directory, "config.js");

            if (!File.Exists(file))
            {
                return new TConfigurationType();
            }

            return JsonSerializer.DeserializeFromFile<TConfigurationType>(file);
        }
    }
}
