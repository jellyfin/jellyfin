#pragma warning disable SA1402

using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Provides a common base class for all plugins.
    /// </summary>
    public abstract class BasePlugin : IPlugin, IPluginAssembly
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public virtual string Description => string.Empty;

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public virtual Guid Id { get; private set; }

        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        /// <value>The version.</value>
        public Version Version { get; private set; }

        /// <summary>
        /// Gets the path to the assembly file.
        /// </summary>
        /// <value>The assembly file path.</value>
        public string AssemblyFilePath { get; private set; }

        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed.
        /// </summary>
        /// <value>The data folder path.</value>
        public string DataFolderPath { get; private set; }

        /// <summary>
        /// Gets the plugin info.
        /// </summary>
        /// <returns>PluginInfo.</returns>
        public virtual PluginInfo GetPluginInfo()
        {
            var info = new PluginInfo
            {
                Name = Name,
                Version = Version.ToString(),
                Description = Description,
                Id = Id.ToString()
            };

            return info;
        }

        /// <summary>
        /// Called just before the plugin is uninstalled from the server.
        /// </summary>
        public virtual void OnUninstalling()
        {
        }

        /// <inheritdoc />
        public void SetAttributes(string assemblyFilePath, string dataFolderPath, Version assemblyVersion)
        {
            AssemblyFilePath = assemblyFilePath;
            DataFolderPath = dataFolderPath;
            Version = assemblyVersion;
        }

        /// <inheritdoc />
        public void SetId(Guid assemblyId)
        {
            Id = assemblyId;
        }
    }

    /// <summary>
    /// Provides a common base class for all plugins.
    /// </summary>
    /// <typeparam name="TConfigurationType">The type of the T configuration type.</typeparam>
    public abstract class BasePlugin<TConfigurationType> : BasePlugin, IHasPluginConfiguration
        where TConfigurationType : BasePluginConfiguration
    {
        /// <summary>
        /// The configuration sync lock.
        /// </summary>
        private readonly object _configurationSyncLock = new object();

        /// <summary>
        /// The configuration save lock.
        /// </summary>
        private readonly object _configurationSaveLock = new object();

        private Action<string> _directoryCreateFn;

        /// <summary>
        /// The configuration.
        /// </summary>
        private TConfigurationType _configuration;

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
        /// Gets the type of configuration this plugin uses.
        /// </summary>
        /// <value>The type of the configuration.</value>
        public Type ConfigurationType => typeof(TConfigurationType);

        /// <summary>
        /// Gets the name the assembly file.
        /// </summary>
        /// <value>The name of the assembly file.</value>
        protected string AssemblyFileName => Path.GetFileName(AssemblyFilePath);

        /// <summary>
        /// Gets or sets the plugin configuration.
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

            protected set => _configuration = value;
        }

        /// <summary>
        /// Gets the name of the configuration file. Subclasses should override.
        /// </summary>
        /// <value>The name of the configuration file.</value>
        public virtual string ConfigurationFileName => Path.ChangeExtension(AssemblyFileName, ".xml");

        /// <summary>
        /// Gets the full path to the configuration file.
        /// </summary>
        /// <value>The configuration file path.</value>
        public string ConfigurationFilePath => Path.Combine(ApplicationPaths.PluginConfigurationsPath, ConfigurationFileName);

        /// <summary>
        /// Gets the plugin configuration.
        /// </summary>
        /// <value>The configuration.</value>
        BasePluginConfiguration IHasPluginConfiguration.Configuration => Configuration;

        /// <inheritdoc />
        public void SetStartupInfo(Action<string> directoryCreateFn)
        {
            // hack alert, until the .net core transition is complete
            _directoryCreateFn = directoryCreateFn;
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
        /// Saves the current configuration to the file system.
        /// </summary>
        public virtual void SaveConfiguration()
        {
            lock (_configurationSaveLock)
            {
                _directoryCreateFn(Path.GetDirectoryName(ConfigurationFilePath));

                XmlSerializer.SerializeToFile(Configuration, ConfigurationFilePath);
            }
        }

        /// <inheritdoc />
        public virtual void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Configuration = (TConfigurationType)configuration;

            SaveConfiguration();
        }

        /// <inheritdoc />
        public override PluginInfo GetPluginInfo()
        {
            var info = base.GetPluginInfo();

            info.ConfigurationFileName = ConfigurationFileName;

            return info;
        }
    }
}
