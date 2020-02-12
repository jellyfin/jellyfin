using System;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class User
    /// </summary>
    public class User : BaseItem
    {
        public static IUserManager UserManager { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }
        public string EasyPassword { get; set; }

        // Strictly to remove JsonIgnore
        public override ItemImageInfo[] ImageInfos
        {
            get => base.ImageInfos;
            set => base.ImageInfos = value;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [JsonIgnore]
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
        [JsonIgnore]
        public override string ContainingFolderPath => Path;

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        [JsonIgnore]
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
        [JsonIgnore]
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
        [JsonIgnore]
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
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Username can't be empty", nameof(newName));
            }

            Name = newName;

            return RefreshMetadata(
                new MetadataRefreshOptions(new DirectoryService(FileSystem))
                {
                    ReplaceAllMetadata = true,
                    ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ForceSave = true

                },
                CancellationToken.None);
        }

        public override void UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            UserManager.UpdateUser(this);
        }

        /// <summary>
        /// Gets the path to the user's configuration directory
        /// </summary>
        /// <value>The configuration directory path.</value>
        [JsonIgnore]
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

            // TODO: Remove idPath and just use usernamePath for future releases
            var usernamePath = System.IO.Path.Combine(parentPath, username);
            var idPath = System.IO.Path.Combine(parentPath, Id.ToString("N", CultureInfo.InvariantCulture));
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

        [JsonIgnore]
        public override bool SupportsPeople => false;

        public long InternalId { get; set; }


    }
}
