using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class User
    /// </summary>
    public class User : BaseItem
    {
        public static IUserManager UserManager { get; set; }
        public static IXmlSerializer XmlSerializer { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [IgnoreDataMember]
        public override string Path
        {
            get
            {
                // Return this so that metadata providers will look in here
                return ConfigurationDirectoryPath;
            }
            set
            {
                base.Path = value;
            }
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        public override string ContainingFolderPath
        {
            get
            {
                return Path;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        [IgnoreDataMember]
        public Folder RootFolder
        {
            get
            {
                return LibraryManager.GetUserRootFolder();
            }
        }

        /// <summary>
        /// Gets or sets the last login date.
        /// </summary>
        /// <value>The last login date.</value>
        public DateTime? LastLoginDate { get; set; }
        /// <summary>
        /// Gets or sets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        public DateTime? LastActivityDate { get; set; }

        /// <summary>
        /// The _configuration
        /// </summary>
        private UserConfiguration _configuration;
        /// <summary>
        /// The _configuration initialized
        /// </summary>
        private bool _configurationInitialized;
        /// <summary>
        /// The _configuration sync lock
        /// </summary>
        private object _configurationSyncLock = new object();
        /// <summary>
        /// Gets the user's configuration
        /// </summary>
        /// <value>The configuration.</value>
        [IgnoreDataMember]
        public UserConfiguration Configuration
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _configuration, ref _configurationInitialized, ref _configurationSyncLock, () => (UserConfiguration)ConfigurationHelper.GetXmlConfiguration(typeof(UserConfiguration), ConfigurationFilePath, XmlSerializer));
                return _configuration;
            }
            private set
            {
                _configuration = value;

                _configurationInitialized = value != null;
            }
        }

        /// <summary>
        /// Renames the user.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Task Rename(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentNullException();
            }

            // If only the casing is changing, leave the file system alone
            if (!newName.Equals(Name, StringComparison.OrdinalIgnoreCase))
            {
                // Move configuration
                var newConfigDirectory = GetConfigurationDirectoryPath(newName);
                var oldConfigurationDirectory = ConfigurationDirectoryPath;

                // Exceptions will be thrown if these paths already exist
                if (Directory.Exists(newConfigDirectory))
                {
                    Directory.Delete(newConfigDirectory, true);
                }

                if (Directory.Exists(oldConfigurationDirectory))
                {
                    Directory.Move(oldConfigurationDirectory, newConfigDirectory);
                }
                else
                {
                    Directory.CreateDirectory(newConfigDirectory);
                }
            }

            Name = newName;

            return RefreshMetadata(new MetadataRefreshOptions
            {
                ReplaceAllMetadata = true,
                ImageRefreshMode = ImageRefreshMode.FullRefresh,
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh

            }, CancellationToken.None);
        }

        public override Task UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            return UserManager.UpdateUser(this);
        }

        /// <summary>
        /// Gets the path to the user's configuration directory
        /// </summary>
        /// <value>The configuration directory path.</value>
        [IgnoreDataMember]
        public string ConfigurationDirectoryPath
        {
            get
            {
                return GetConfigurationDirectoryPath(Name);
            }
        }

        /// <summary>
        /// Gets the configuration directory path.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>System.String.</returns>
        private string GetConfigurationDirectoryPath(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException("username");
            }

            var safeFolderName = FileSystem.GetValidFilename(username);

            return System.IO.Path.Combine(ConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath, safeFolderName);
        }

        /// <summary>
        /// Gets the path to the user's configuration file
        /// </summary>
        /// <value>The configuration file path.</value>
        [IgnoreDataMember]
        public string ConfigurationFilePath
        {
            get
            {
                return System.IO.Path.Combine(ConfigurationDirectoryPath, "config.xml");
            }
        }

        /// <summary>
        /// Saves the current configuration to the file system
        /// </summary>
        public void SaveConfiguration()
        {
            var xmlPath = ConfigurationFilePath;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(xmlPath));
            XmlSerializer.SerializeToFile(Configuration, xmlPath);
        }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="serializer">The serializer.</param>
        /// <exception cref="System.ArgumentNullException">config</exception>
        public void UpdateConfiguration(UserConfiguration config, IXmlSerializer serializer)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Configuration = config;
            SaveConfiguration();
        }
    }
}
