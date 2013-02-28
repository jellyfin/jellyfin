using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class User
    /// </summary>
    public class User : BaseItem
    {
        internal static IUserManager UserManager { get; set; }

        /// <summary>
        /// The _root folder path
        /// </summary>
        private string _rootFolderPath;
        /// <summary>
        /// Gets the root folder path.
        /// </summary>
        /// <value>The root folder path.</value>
        [IgnoreDataMember]
        public string RootFolderPath
        {
            get
            {
                if (_rootFolderPath == null)
                {
                    if (Configuration.UseCustomLibrary)
                    {
                        _rootFolderPath = GetRootFolderPath(Name);

                        if (!Directory.Exists(_rootFolderPath))
                        {
                            Directory.CreateDirectory(_rootFolderPath);
                        }
                    }
                    else
                    {
                        _rootFolderPath = Kernel.Instance.ApplicationPaths.DefaultUserViewsPath;
                    }
                }
                return _rootFolderPath;
            }
        }

        /// <summary>
        /// Gets the root folder path based on a given username
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>System.String.</returns>
        private string GetRootFolderPath(string username)
        {
            var safeFolderName = FileSystem.GetValidFilename(username);

            return System.IO.Path.Combine(Kernel.Instance.ApplicationPaths.RootFolderPath, safeFolderName);
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
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
        /// Ensure this has a value
        /// </summary>
        /// <value>The display type of the media.</value>
        public override string DisplayMediaType
        {
            get
            {
                return base.DisplayMediaType ?? GetType().Name;
            }
            set
            {
                base.DisplayMediaType = value;
            }
        }

        /// <summary>
        /// The _root folder
        /// </summary>
        private UserRootFolder _rootFolder;
        /// <summary>
        /// The _user root folder initialized
        /// </summary>
        private bool _userRootFolderInitialized;
        /// <summary>
        /// The _user root folder sync lock
        /// </summary>
        private object _userRootFolderSyncLock = new object();
        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        [IgnoreDataMember]
        public UserRootFolder RootFolder
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _rootFolder, ref _userRootFolderInitialized, ref _userRootFolderSyncLock, () => (UserRootFolder)LibraryManager.ResolvePath(RootFolderPath));
                return _rootFolder;
            }
            private set
            {
                _rootFolder = value;

                if (_rootFolder == null)
                {
                    _userRootFolderInitialized = false;
                }
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
                LazyInitializer.EnsureInitialized(ref _configuration, ref _configurationInitialized, ref _configurationSyncLock, () => (UserConfiguration)Kernel.Instance.GetXmlConfiguration(typeof(UserConfiguration), ConfigurationFilePath));
                return _configuration;
            }
            private set
            {
                _configuration = value;

                if (value == null)
                {
                    _configurationInitialized = false;
                }
            }
        }

        /// <summary>
        /// Gets the last date modified of the configuration
        /// </summary>
        /// <value>The configuration date last modified.</value>
        [IgnoreDataMember]
        public DateTime ConfigurationDateLastModified
        {
            get
            {
                // Ensure it's been lazy loaded
                var config = Configuration;

                return File.GetLastWriteTimeUtc(ConfigurationFilePath);
            }
        }

        /// <summary>
        /// Reloads the root media folder
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task ValidateMediaLibrary(IProgress<double> progress, CancellationToken cancellationToken)
        {
            Logger.Info("Validating media library for {0}", Name);
            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            await RootFolder.ValidateChildren(progress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Validates only the collection folders for a User and goes no further
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task ValidateCollectionFolders(IProgress<double> progress, CancellationToken cancellationToken)
        {
            Logger.Info("Validating collection folders for {0}", Name);
            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            await RootFolder.ValidateChildren(progress, cancellationToken, recursive: false).ConfigureAwait(false);
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

                // Exceptions will be thrown if these paths already exist
                if (Directory.Exists(newConfigDirectory))
                {
                    Directory.Delete(newConfigDirectory, true);
                }
                Directory.Move(ConfigurationDirectoryPath, newConfigDirectory);

                var customLibraryPath = GetRootFolderPath(Name);

                // Move the root folder path if using a custom library
                if (Directory.Exists(customLibraryPath))
                {
                    var newRootFolderPath = GetRootFolderPath(newName);
                    if (Directory.Exists(newRootFolderPath))
                    {
                        Directory.Delete(newRootFolderPath, true);
                    }
                    Directory.Move(customLibraryPath, newRootFolderPath);
                }
            }

            Name = newName;

            // Force these to be lazy loaded again
            _configurationDirectoryPath = null;
            _rootFolderPath = null;
            RootFolder = null;

            // Kick off a task to validate the media library
            Task.Run(() => ValidateMediaLibrary(new Progress<double> { }, CancellationToken.None));

            return RefreshMetadata(CancellationToken.None, forceSave: true, forceRefresh: true);
        }

        /// <summary>
        /// The _configuration directory path
        /// </summary>
        private string _configurationDirectoryPath;
        /// <summary>
        /// Gets the path to the user's configuration directory
        /// </summary>
        /// <value>The configuration directory path.</value>
        private string ConfigurationDirectoryPath
        {
            get
            {
                if (_configurationDirectoryPath == null)
                {
                    _configurationDirectoryPath = GetConfigurationDirectoryPath(Name);

                    if (!Directory.Exists(_configurationDirectoryPath))
                    {
                        Directory.CreateDirectory(_configurationDirectoryPath);
                    }
                }

                return _configurationDirectoryPath;
            }
        }

        /// <summary>
        /// Gets the configuration directory path.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>System.String.</returns>
        private string GetConfigurationDirectoryPath(string username)
        {
            var safeFolderName = FileSystem.GetValidFilename(username);

            return System.IO.Path.Combine(Kernel.Instance.ApplicationPaths.UserConfigurationDirectoryPath, safeFolderName);
        }

        /// <summary>
        /// Gets the path to the user's configuration file
        /// </summary>
        /// <value>The configuration file path.</value>
        private string ConfigurationFilePath
        {
            get
            {
                return System.IO.Path.Combine(ConfigurationDirectoryPath, "config.xml");
            }
        }

        /// <summary>
        /// Saves the current configuration to the file system
        /// </summary>
        public void SaveConfiguration(IXmlSerializer serializer)
        {
            serializer.SerializeToFile(Configuration, ConfigurationFilePath);
        }

        /// <summary>
        /// Refresh metadata on us by execution our provider chain
        /// The item will be persisted if a change is made by a provider, or if it's new or changed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="forceSave">if set to <c>true</c> [is new item].</param>
        /// <param name="forceRefresh">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="resetResolveArgs">if set to <c>true</c> [reset resolve args].</param>
        /// <returns>true if a provider reports we changed</returns>
        public override async Task<bool> RefreshMetadata(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true, bool resetResolveArgs = true)
        {
            if (resetResolveArgs)
            {
                ResolveArgs = null;
            }

            var changed = await Kernel.Instance.ProviderManager.ExecuteMetadataProviders(this, cancellationToken, forceRefresh, allowSlowProviders).ConfigureAwait(false);

            if (changed || forceSave)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await UserManager.UpdateUser(this).ConfigureAwait(false);
            }

            return changed;
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

            var customLibraryChanged = config.UseCustomLibrary != Configuration.UseCustomLibrary;

            Configuration = config;
            SaveConfiguration(serializer);

            // Force these to be lazy loaded again
            if (customLibraryChanged)
            {
                _rootFolderPath = null;
                RootFolder = null;

                if (config.UseCustomLibrary)
                {
                    CopyDefaultLibraryPathsIfNeeded();
                }
            }
        }

        /// <summary>
        /// Copies the default library paths if needed.
        /// </summary>
        private void CopyDefaultLibraryPathsIfNeeded()
        {
            var userPath = RootFolderPath;

            var defaultPath = Kernel.Instance.ApplicationPaths.DefaultUserViewsPath;

            if (userPath.Equals(defaultPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!Directory.EnumerateFileSystemEntries(userPath, "*.lnk", SearchOption.AllDirectories).Any())
            {
                FileSystem.CopyAll(defaultPath, userPath);
            }
        }

        /// <summary>
        /// Resets the password by clearing it.
        /// </summary>
        /// <returns>Task.</returns>
        public Task ResetPassword(IUserManager userManager)
        {
            return ChangePassword(string.Empty, userManager);
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="newPassword">The new password.</param>
        /// <returns>Task.</returns>
        public Task ChangePassword(string newPassword, IUserManager userManager)
        {
            Password = string.IsNullOrEmpty(newPassword) ? string.Empty : newPassword.GetMD5().ToString();

            return userManager.UpdateUser(this);
        }
    }
}
