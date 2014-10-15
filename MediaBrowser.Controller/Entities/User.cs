using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using System.Linq;
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
        /// From now on all user paths will be Id-based. 
        /// This is for backwards compatibility.
        /// </summary>
        public bool UsesIdForConfigurationPath { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }
        public string LocalPassword { get; set; }

        public string ConnectUserName { get; set; }
        public string ConnectUserId { get; set; }
        public UserLinkType? ConnectLinkType { get; set; }
        public string ConnectAccessKey { get; set; }

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
                throw new ArgumentNullException("newName");
            }

            // If only the casing is changing, leave the file system alone
            if (!UsesIdForConfigurationPath && !newName.Equals(Name, StringComparison.OrdinalIgnoreCase))
            {
                UsesIdForConfigurationPath = true;

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
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                ForceSave = true

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
        private string ConfigurationDirectoryPath
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

            var parentPath = ConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath;

            // Legacy
            if (!UsesIdForConfigurationPath)
            {
                var safeFolderName = FileSystem.GetValidFilename(username);

                return System.IO.Path.Combine(ConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath, safeFolderName);
            }

            return System.IO.Path.Combine(parentPath, Id.ToString("N"));
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
        /// Updates the configuration.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <exception cref="System.ArgumentNullException">config</exception>
        public void UpdateConfiguration(UserConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Configuration = config;
            UserManager.UpdateConfiguration(this, Configuration);
        }

        public bool IsParentalScheduleAllowed()
        {
            return IsParentalScheduleAllowed(DateTime.UtcNow);
        }

        public bool IsParentalScheduleAllowed(DateTime date)
        {
            var schedules = Configuration.AccessSchedules;

            if (schedules.Length == 0)
            {
                return true;
            }

            return schedules.Any(i => IsParentalScheduleAllowed(i, date));
        }

        private bool IsParentalScheduleAllowed(AccessSchedule schedule, DateTime date)
        {
            if (date.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Utc date expected");
            }

            var localTime = date.ToLocalTime();

            return localTime.DayOfWeek == schedule.DayOfWeek && IsWithinTime(schedule, localTime);
        }

        private bool IsWithinTime(AccessSchedule schedule, DateTime localTime)
        {
            var hour = localTime.TimeOfDay.TotalHours;

            return hour >= schedule.StartHour && hour <= schedule.EndHour;
        }
    }
}
