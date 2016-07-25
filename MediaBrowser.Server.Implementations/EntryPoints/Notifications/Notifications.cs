using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;

namespace MediaBrowser.Server.Implementations.EntryPoints.Notifications
{
    /// <summary>
    /// Creates notifications for various system events
    /// </summary>
    public class Notifications : IServerEntryPoint
    {
        private readonly IInstallationManager _installationManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        private readonly ITaskManager _taskManager;
        private readonly INotificationManager _notificationManager;

        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;
        private readonly IServerApplicationHost _appHost;

        private Timer LibraryUpdateTimer { get; set; }
        private readonly object _libraryChangedSyncLock = new object();

        private readonly IConfigurationManager _config;
        private readonly IDeviceManager _deviceManager;

        public Notifications(IInstallationManager installationManager, IUserManager userManager, ILogger logger, ITaskManager taskManager, INotificationManager notificationManager, ILibraryManager libraryManager, ISessionManager sessionManager, IServerApplicationHost appHost, IConfigurationManager config, IDeviceManager deviceManager)
        {
            _installationManager = installationManager;
            _userManager = userManager;
            _logger = logger;
            _taskManager = taskManager;
            _notificationManager = notificationManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _appHost = appHost;
            _config = config;
            _deviceManager = deviceManager;
        }

        public void Run()
        {
            _installationManager.PluginInstalled += _installationManager_PluginInstalled;
            _installationManager.PluginUpdated += _installationManager_PluginUpdated;
            _installationManager.PackageInstallationFailed += _installationManager_PackageInstallationFailed;
            _installationManager.PluginUninstalled += _installationManager_PluginUninstalled;

            _taskManager.TaskCompleted += _taskManager_TaskCompleted;

            _userManager.UserCreated += _userManager_UserCreated;
            _libraryManager.ItemAdded += _libraryManager_ItemAdded;
            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped += _sessionManager_PlaybackStopped;
            _appHost.HasPendingRestartChanged += _appHost_HasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged += _appHost_HasUpdateAvailableChanged;
            _appHost.ApplicationUpdated += _appHost_ApplicationUpdated;
            _deviceManager.CameraImageUploaded += _deviceManager_CameraImageUploaded;

            _userManager.UserLockedOut += _userManager_UserLockedOut;
        }

        async void _userManager_UserLockedOut(object sender, GenericEventArgs<User> e)
        {
            var type = NotificationType.UserLockedOut.ToString();

            var notification = new NotificationRequest
            {
                NotificationType = type
            };

            notification.Variables["UserName"] = e.Argument.Name;

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _deviceManager_CameraImageUploaded(object sender, GenericEventArgs<CameraImageUploadInfo> e)
        {
            var type = NotificationType.CameraImageUploaded.ToString();

            var notification = new NotificationRequest
            {
                NotificationType = type
            };

            notification.Variables["DeviceName"] = e.Argument.Device.Name;

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _appHost_ApplicationUpdated(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            var type = NotificationType.ApplicationUpdateInstalled.ToString();

            var notification = new NotificationRequest
            {
                NotificationType = type,
                Url = e.Argument.infoUrl
            };

            notification.Variables["Version"] = e.Argument.versionStr;
            notification.Variables["ReleaseNotes"] = e.Argument.description;

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _installationManager_PluginUpdated(object sender, GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> e)
        {
            var type = NotificationType.PluginUpdateInstalled.ToString();

            var installationInfo = e.Argument.Item1;

            var notification = new NotificationRequest
            {
                Description = e.Argument.Item2.description,
                NotificationType = type
            };

            notification.Variables["Name"] = installationInfo.Name;
            notification.Variables["Version"] = installationInfo.Version.ToString();
            notification.Variables["ReleaseNotes"] = e.Argument.Item2.description;

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _installationManager_PluginInstalled(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            var type = NotificationType.PluginInstalled.ToString();

            var installationInfo = e.Argument;

            var notification = new NotificationRequest
            {
                Description = installationInfo.description,
                NotificationType = type
            };

            notification.Variables["Name"] = installationInfo.name;
            notification.Variables["Version"] = installationInfo.versionStr;

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _appHost_HasUpdateAvailableChanged(object sender, EventArgs e)
        {
            // This notification is for users who can't auto-update (aka running as service)
            if (!_appHost.HasUpdateAvailable || _appHost.CanSelfUpdate)
            {
                return;
            }

            var type = NotificationType.ApplicationUpdateAvailable.ToString();

            var notification = new NotificationRequest
            {
                Description = "Please see emby.media for details.",
                NotificationType = type
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _appHost_HasPendingRestartChanged(object sender, EventArgs e)
        {
            if (!_appHost.HasPendingRestart)
            {
                return;
            }

            var type = NotificationType.ServerRestartRequired.ToString();

            var notification = new NotificationRequest
            {
                NotificationType = type
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        private NotificationOptions GetOptions()
        {
            return _config.GetConfiguration<NotificationOptions>("notifications");
        }

        void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                _logger.Warn("PlaybackStart reported with null media info.");
                return;
            }

            var video = e.Item as Video;
            if (video != null && video.IsThemeMedia)
            {
                return;
            }

            var type = GetPlaybackNotificationType(item.MediaType);

            SendPlaybackNotification(type, e);
        }

        void _sessionManager_PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                _logger.Warn("PlaybackStopped reported with null media info.");
                return;
            }

            var video = e.Item as Video;
            if (video != null && video.IsThemeMedia)
            {
                return;
            }

            var type = GetPlaybackStoppedNotificationType(item.MediaType);

            SendPlaybackNotification(type, e);
        }

        private async void SendPlaybackNotification(string type, PlaybackProgressEventArgs e)
        {
            var user = e.Users.FirstOrDefault();

            if (user != null && !GetOptions().IsEnabledToMonitorUser(type, user.Id.ToString("N")))
            {
                return;
            }

            var item = e.MediaInfo;
            var themeMedia = item as IThemeMedia;

            if (themeMedia != null && themeMedia.IsThemeMedia)
            {
                // Don't report theme song or local trailer playback
                return;
            }

            var notification = new NotificationRequest
            {
                NotificationType = type
            };

            if (e.Item != null)
            {
                notification.Variables["ItemName"] = GetItemName(e.Item);
            }
            else
            {
                notification.Variables["ItemName"] = item.Name;
            }

            notification.Variables["UserName"] = user == null ? "Unknown user" : user.Name;
            notification.Variables["AppName"] = e.ClientName;
            notification.Variables["DeviceName"] = e.DeviceName;

            await SendNotification(notification).ConfigureAwait(false);
        }

        private string GetPlaybackNotificationType(string mediaType)
        {
            if (string.Equals(mediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.AudioPlayback.ToString();
            }
            if (string.Equals(mediaType, MediaType.Game, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.GamePlayback.ToString();
            }
            if (string.Equals(mediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.VideoPlayback.ToString();
            }

            return null;
        }

        private string GetPlaybackStoppedNotificationType(string mediaType)
        {
            if (string.Equals(mediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.AudioPlaybackStopped.ToString();
            }
            if (string.Equals(mediaType, MediaType.Game, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.GamePlaybackStopped.ToString();
            }
            if (string.Equals(mediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.VideoPlaybackStopped.ToString();
            }

            return null;
        }

        private readonly List<BaseItem> _itemsAdded = new List<BaseItem>();
        void _libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, 5000,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(5000, Timeout.Infinite);
                }

                _itemsAdded.Add(e.Item);
            }
        }

        private bool FilterItem(BaseItem item)
        {
            if (item.IsFolder)
            {
                return false;
            }

            if (item.LocationType == LocationType.Virtual)
            {
                return false;
            }

            if (item is IItemByName)
            {
                return false;
            }

            return item.SourceType == SourceType.Library;
        }

        private async void LibraryUpdateTimerCallback(object state)
        {
            List<BaseItem> items;

            lock (_libraryChangedSyncLock)
            {
                items = _itemsAdded.ToList();
                _itemsAdded.Clear();
                DisposeLibraryUpdateTimer();
            }

            if (items.Count == 1)
            {
                var item = items.First();

                var notification = new NotificationRequest
                {
                    NotificationType = NotificationType.NewLibraryContent.ToString()
                };

                notification.Variables["Name"] = GetItemName(item);

                await SendNotification(notification).ConfigureAwait(false);
            }
            else
            {
                var notification = new NotificationRequest
                {
                    NotificationType = NotificationType.NewLibraryContentMultiple.ToString()
                };

                notification.Variables["ItemCount"] = items.Count.ToString(CultureInfo.InvariantCulture);

                await SendNotification(notification).ConfigureAwait(false);
            }
        }

        public static string GetItemName(BaseItem item)
        {
            var name = item.Name;
            var episode = item as Episode;
            if (episode != null)
            {
                if (episode.IndexNumber.HasValue)
                {
                    name = string.Format("Ep{0} - {1}", episode.IndexNumber.Value.ToString(CultureInfo.InvariantCulture), name);
                }
                if (episode.ParentIndexNumber.HasValue)
                {
                    name = string.Format("S{0}, {1}", episode.ParentIndexNumber.Value.ToString(CultureInfo.InvariantCulture), name);
                }
            }

            var hasSeries = item as IHasSeries;

            if (hasSeries != null)
            {
                name = hasSeries.SeriesName + " - " + name;
            }

            var hasArtist = item as IHasArtist;
            if (hasArtist != null)
            {
                var artists = hasArtist.AllArtists;

                if (artists.Count > 0)
                {
                    name = hasArtist.AllArtists[0] + " - " + name;
                }
            }

            return name;
        }

        async void _userManager_UserCreated(object sender, GenericEventArgs<User> e)
        {
            var notification = new NotificationRequest
            {
                UserIds = new List<string> { e.Argument.Id.ToString("N") },
                Name = "Welcome to Emby!",
                Description = "Check back here for more notifications."
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _taskManager_TaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            var result = e.Result;

            if (result.Status == TaskCompletionStatus.Failed)
            {
                var type = NotificationType.TaskFailed.ToString();

                var notification = new NotificationRequest
                {
                    Description = result.ErrorMessage,
                    Level = NotificationLevel.Error,
                    NotificationType = type
                };

                notification.Variables["Name"] = result.Name;
                notification.Variables["ErrorMessage"] = result.ErrorMessage;

                await SendNotification(notification).ConfigureAwait(false);
            }
        }

        async void _installationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            var type = NotificationType.PluginUninstalled.ToString();

            var plugin = e.Argument;

            var notification = new NotificationRequest
            {
                NotificationType = type
            };

            notification.Variables["Name"] = plugin.Name;
            notification.Variables["Version"] = plugin.Version.ToString();

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _installationManager_PackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            var type = NotificationType.InstallationFailed.ToString();

            var notification = new NotificationRequest
            {
                Level = NotificationLevel.Error,
                Description = e.Exception.Message,
                NotificationType = type
            };

            notification.Variables["Name"] = installationInfo.Name;
            notification.Variables["Version"] = installationInfo.Version;

            await SendNotification(notification).ConfigureAwait(false);
        }

        private async Task SendNotification(NotificationRequest notification)
        {
            try
            {
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending notification", ex);
            }
        }

        public void Dispose()
        {
            DisposeLibraryUpdateTimer();

            _installationManager.PluginInstalled -= _installationManager_PluginInstalled;
            _installationManager.PluginUpdated -= _installationManager_PluginUpdated;
            _installationManager.PackageInstallationFailed -= _installationManager_PackageInstallationFailed;
            _installationManager.PluginUninstalled -= _installationManager_PluginUninstalled;

            _taskManager.TaskCompleted -= _taskManager_TaskCompleted;

            _userManager.UserCreated -= _userManager_UserCreated;
            _libraryManager.ItemAdded -= _libraryManager_ItemAdded;
            _sessionManager.PlaybackStart -= _sessionManager_PlaybackStart;

            _appHost.HasPendingRestartChanged -= _appHost_HasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged -= _appHost_HasUpdateAvailableChanged;
            _appHost.ApplicationUpdated -= _appHost_ApplicationUpdated;

            _deviceManager.CameraImageUploaded -= _deviceManager_CameraImageUploaded;
            _userManager.UserLockedOut -= _userManager_UserLockedOut;
        }

        private void DisposeLibraryUpdateTimer()
        {
            if (LibraryUpdateTimer != null)
            {
                LibraryUpdateTimer.Dispose();
                LibraryUpdateTimer = null;
            }
        }
    }
}
