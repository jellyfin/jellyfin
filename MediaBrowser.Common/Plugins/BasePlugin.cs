using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Provides a common base class for all plugins
    /// </summary>
    /// <typeparam name="TConfigurationType">The type of the T configuration type.</typeparam>
    public abstract class BasePlugin<TConfigurationType> : IPlugin, IPluginAssembly
        where TConfigurationType : BasePluginConfiguration
    {
        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        protected IApplicationPaths ApplicationPaths { get; private set; }

        /// <summary>
        /// Gets the XML serializer.
        /// </summary>
        /// <value>The XML serializer.</value>
        protected IXmlSerializer XmlSerializer { get; private set; }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        public bool IsFirstRun { get; private set; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public virtual string Description
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the type of configuration this plugin uses
        /// </summary>
        /// <value>The type of the configuration.</value>
        public Type ConfigurationType
        {
            get { return typeof(TConfigurationType); }
        }

        public void SetAttributes(string assemblyFilePath, string assemblyFileName, Version assemblyVersion, Guid assemblyId)
        {
            AssemblyFilePath = assemblyFilePath;
            AssemblyFileName = assemblyFileName;
            Version = assemblyVersion;
            Id = assemblyId;
        }

        private Func<string, DateTime> _dateModifiedFn;
        private Action<string> _directoryCreateFn;
        public void SetStartupInfo(bool isFirstRun, Func<string, DateTime> dateModifiedFn, Action<string> directoryCreateFn)
        {
            IsFirstRun = isFirstRun;

            // hack alert, until the .net core transition is complete
            _dateModifiedFn = dateModifiedFn;
            _directoryCreateFn = directoryCreateFn;
        }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the plugin version
        /// </summary>
        /// <value>The version.</value>
        public Version Version { get; private set; }

        /// <summary>
        /// Gets the name the assembly file
        /// </summary>
        /// <value>The name of the assembly file.</value>
        protected string AssemblyFileName { get; private set; }

        /// <summary>
        /// Gets the last date modified of the configuration
        /// </summary>
        /// <value>The configuration date last modified.</value>
        public DateTime ConfigurationDateLastModified
        {
            get
            {
                // Ensure it's been lazy loaded
                var config = Configuration;

                return _dateModifiedFn(ConfigurationFilePath);
            }
        }

        /// <summary>
        /// Gets the path to the assembly file
        /// </summary>
        /// <value>The assembly file path.</value>
        public string AssemblyFilePath { get; private set; }

        /// <summary>
        /// The _configuration sync lock
        /// </summary>
        private readonly object _configurationSyncLock = new object();
        /// <summary>
        /// The _configuration
        /// </summary>
        private TConfigurationType _configuration;
        /// <summary>
        /// Gets the plugin's configuration
        /// </summary>
        /// <value>The configuration.</value>
        public TConfigurationType Configuration
        {
            get
            {
                // Lazy load
                if (_configuration == null)
                {
                    lock (_configurationSyncLock)
                    {
                        if (_configuration == null)
                        {
                            _configuration = LoadConfiguration();
                        }
                    }
                } 
                return _configuration;
            }
            protected set
            {
                _configuration = value;
            }
        }

        private TConfigurationType LoadConfiguration()
        {
            var path = ConfigurationFilePath;

            try
            {
                return (TConfigurationType)XmlSerializer.DeserializeFromFile(typeof(TConfigurationType), path);
            }
            catch
            {
                return (TConfigurationType)Activator.CreateInstance(typeof(TConfigurationType));
            }
        }

        /// <summary>
        /// Gets the name of the configuration file. Subclasses should override
        /// </summary>
        /// <value>The name of the configuration file.</value>
        public virtual string ConfigurationFileName
        {
            get { return Path.ChangeExtension(AssemblyFileName, ".xml"); }
        }

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        /// <value>The configuration file path.</value>
        public string ConfigurationFilePath
        {
            get
            {
                return Path.Combine(ApplicationPaths.PluginConfigurationsPath, ConfigurationFileName);
            }
        }

        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed
        /// </summary>
        /// <value>The data folder path.</value>
        public string DataFolderPath
        {
            get
            {
                // Give the folder name the same name as the config file name
                // We can always make this configurable if/when needed
                return Path.Combine(ApplicationPaths.PluginsPath, Path.GetFileNameWithoutExtension(ConfigurationFileName));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePlugin{TConfigurationType}" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        protected BasePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        {
            ApplicationPaths = applicationPaths;
            XmlSerializer = xmlSerializer;
        }

        /// <summary>
        /// The _save lock
        /// </summary>
        private readonly object _configurationSaveLock = new object();

        /// <summary>
        /// Saves the current configuration to the file system
        /// </summary>
        public virtual void SaveConfiguration()
        {
            lock (_configurationSaveLock)
            {
                _directoryCreateFn(Path.GetDirectoryName(ConfigurationFilePath));
                
                XmlSerializer.SerializeToFile(Configuration, ConfigurationFilePath);
            }
        }

        /// <summary>
        /// Completely overwrites the current configuration with a new copy
        /// Returns true or false indicating success or failure
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">configuration</exception>
        public virtual void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            Configuration = (TConfigurationType)configuration;

            SaveConfiguration();
        }

        /// <summary>
        /// Gets the plugin info.
        /// </summary>
        /// <returns>PluginInfo.</returns>
        public PluginInfo GetPluginInfo()
        {
            var info = new PluginInfo
            {
                Name = Name,
                Version = Version.ToString(),
                AssemblyFileName = AssemblyFileName,
                ConfigurationDateLastModified = ConfigurationDateLastModified,
                Description = Description,
                Id = Id.ToString(),
                ConfigurationFileName = ConfigurationFileName
            };

            return info;
        }

        /// <summary>
        /// Called when just before the plugin is uninstalled from the server.
        /// </summary>
        public virtual void OnUninstalling()
        {

        }

        /// <summary>
        /// Gets the plugin's configuration
        /// </summary>
        /// <value>The configuration.</value>
        BasePluginConfiguration IPlugin.Configuration
        {
            get { return Configuration; }
        }
    }

    public interface IPluginAssembly
    {
        void SetAttributes(string assemblyFilePath, string assemblyFileName, Version assemblyVersion, Guid assemblyId);
    }
}
