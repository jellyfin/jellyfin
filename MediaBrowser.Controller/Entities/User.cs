using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;

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
        public string EasyPassword { get; set; }
        public string Salt { get; set; }

        // Strictly to remove IgnoreDataMember
        public override ItemImageInfo[] ImageInfos
        {
            get => base.ImageInfos;
            set => base.ImageInfos = value;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [IgnoreDataMember]
        public override string Path
        {
            get => ConfigurationDirectoryPath;
            set => base.Path = value;
        }

        private string _name;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get => _name;
            set
            {
                _name = value;

                // lazy load this again
                SortName = null;
            }
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        [IgnoreDataMember]
        public override string ContainingFolderPath => Path;

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        [IgnoreDataMember]
        public Folder RootFolder => LibraryManager.GetUserRootFolder();

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

        private volatile UserConfiguration _config;
        private readonly object _configSyncLock = new object();
        [IgnoreDataMember]
        public UserConfiguration Configuration
        {
            get
            {
                if (_config == null)
                {
                    lock (_configSyncLock)
                    {
                        if (_config == null)
                        {
                            _config = UserManager.GetUserConfiguration(this);
                        }
                    }
                }

                return _config;
            }
            set => _config = value;
        }

        private volatile UserPolicy _policy;
        private readonly object _policySyncLock = new object();
        [IgnoreDataMember]
        public UserPolicy Policy
        {
            get
            {
                if (_policy == null)
                {
                    lock (_policySyncLock)
                    {
                        if (_policy == null)
                        {
                            _policy = UserManager.GetUserPolicy(this);
                        }
                    }
                }

                return _policy;
            }
            set => _policy = value;
        }

        /// <summary>
        /// Renames the user.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task Rename(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentNullException(nameof(newName));
            }

            // If only the casing is changing, leave the file system alone
            if (!UsesIdForConfigurationPath && !string.Equals(newName, Name, StringComparison.OrdinalIgnoreCase))
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

            return RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(Logger, FileSystem))
            {
                ReplaceAllMetadata = true,
                ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                ForceSave = true

            }, CancellationToken.None);
        }

        public override void UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            UserManager.UpdateUser(this);
        }

        /// <summary>
        /// Gets the path to the user's configuration directory
        /// </summary>
        /// <value>The configuration directory path.</value>
        [IgnoreDataMember]
        public string ConfigurationDirectoryPath => GetConfigurationDirectoryPath(Name);

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 1;
        }

        /// <summary>
        /// Gets the configuration directory path.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>System.String.</returns>
        private string GetConfigurationDirectoryPath(string username)
        {
            var parentPath = ConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath;

            // Legacy
            if (!UsesIdForConfigurationPath)
            {
                if (string.IsNullOrEmpty(username))
                {
                    throw new ArgumentNullException(nameof(username));
                }

                var safeFolderName = FileSystem.GetValidFilename(username);

                return System.IO.Path.Combine(ConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath, safeFolderName);
            }

            // TODO: Remove idPath and just use usernamePath for future releases
            var usernamePath = System.IO.Path.Combine(parentPath, username);
            var idPath = System.IO.Path.Combine(parentPath, Id.ToString("N"));
            if (!Directory.Exists(usernamePath) && Directory.Exists(idPath))
            {
                Directory.Move(idPath, usernamePath);
            }

            return usernamePath;
        }

        public bool IsParentalScheduleAllowed()
        {
            return IsParentalScheduleAllowed(DateTime.UtcNow);
        }

        public bool IsParentalScheduleAllowed(DateTime date)
        {
            var schedules = Policy.AccessSchedules;

            if (schedules.Length == 0)
            {
                return true;
            }

            foreach (var i in schedules)
            {
                if (IsParentalScheduleAllowed(i, date))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsParentalScheduleAllowed(AccessSchedule schedule, DateTime date)
        {
            if (date.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Utc date expected");
            }

            var localTime = date.ToLocalTime();

            return DayOfWeekHelper.GetDaysOfWeek(schedule.DayOfWeek).Contains(localTime.DayOfWeek) &&
                IsWithinTime(schedule, localTime);
        }

        private bool IsWithinTime(AccessSchedule schedule, DateTime localTime)
        {
            var hour = localTime.TimeOfDay.TotalHours;

            return hour >= schedule.StartHour && hour <= schedule.EndHour;
        }

        public bool IsFolderGrouped(Guid id)
        {
            foreach (var i in Configuration.GroupedFolders)
            {
                if (new Guid(i) == id)
                {
                    return true;
                }
            }
            return false;
        }

        [IgnoreDataMember]
        public override bool SupportsPeople => false;

        public long InternalId { get; set; }


    }
}
