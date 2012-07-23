using System.Configuration;
using System.IO;
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
    public abstract class BaseKernel<TConfigurationContorllerType, TConfigurationType>
        where TConfigurationContorllerType : ConfigurationController<TConfigurationType>, new()
        where TConfigurationType : BaseConfiguration, new()
    {
        /// <summary>
        /// Gets the path to the program data folder
        /// </summary>
        public string ProgramDataPath { get; private set; }

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public TConfigurationContorllerType ConfigurationController { get; private set; }

        /// <summary>
        /// Both the UI and server will have a built-in HttpServer.
        /// People will inevitably want remote control apps so it's needed in the UI too.
        /// </summary>
        public HttpServer HttpServer { get; private set; }

        public PluginController PluginController { get; private set; }

        /// <summary>
        /// Gets the kernel context. The UI kernel will have to override this.
        /// </summary>
        protected KernelContext KernelContext { get { return KernelContext.Server; } }

        public BaseKernel()
        {
            ProgramDataPath = GetProgramDataPath();

            PluginController = new PluginController() { PluginsPath = Path.Combine(ProgramDataPath, "Plugins") };
            ConfigurationController = new TConfigurationContorllerType() { Path = Path.Combine(ProgramDataPath, "config.js") };

            Logger.LoggerInstance = new FileLogger(Path.Combine(ProgramDataPath, "Logs"));
        }

        public virtual void Init()
        {
            ReloadConfiguration();

            ReloadHttpServer();

            ReloadPlugins();
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
            ConfigurationController.Reload();

            Logger.LoggerInstance.LogSeverity = ConfigurationController.Configuration.LogSeverity;
        }

        private void ReloadHttpServer()
        {
            if (HttpServer != null)
            {
                HttpServer.Dispose();
            }

            HttpServer = new HttpServer("http://+:" + ConfigurationController.Configuration.HttpServerPortNumber + "/mediabrowser/");
        }

        protected virtual void ReloadPlugins()
        {
            // Find plugins
            PluginController.Init(KernelContext);
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
