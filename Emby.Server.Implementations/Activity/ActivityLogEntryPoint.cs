using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Authentication;

namespace Emby.Server.Implementations.Activity
{
    public class ActivityLogEntryPoint : IServerEntryPoint
    {
        private readonly IInstallationManager _installationManager;

        //private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly ITaskManager _taskManager;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;

        private readonly ILibraryManager _libraryManager;
        private readonly ISubtitleManager _subManager;
        private readonly IUserManager _userManager;
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly IDeviceManager _deviceManager;

        public ActivityLogEntryPoint(ISessionManager sessionManager, IDeviceManager deviceManager, ITaskManager taskManager, IActivityManager activityManager, ILocalizationManager localization, IInstallationManager installationManager, ILibraryManager libraryManager, ISubtitleManager subManager, IUserManager userManager, IServerConfigurationManager config, IServerApplicationHost appHost)
        {
            _sessionManager = sessionManager;
            _taskManager = taskManager;
            _activityManager = activityManager;
            _localization = localization;
            _installationManager = installationManager;
            _libraryManager = libraryManager;
            _subManager = subManager;
            _userManager = userManager;
            _config = config;
            _appHost = appHost;
            _deviceManager = deviceManager;
        }

        public void Run()
        {
            _taskManager.TaskCompleted += _taskManager_TaskCompleted;

            _installationManager.PluginInstalled += _installationManager_PluginInstalled;
            _installationManager.PluginUninstalled += _installationManager_PluginUninstalled;
            _installationManager.PluginUpdated += _installationManager_PluginUpdated;
            _installationManager.PackageInstallationFailed += _installationManager_PackageInstallationFailed;

            _sessionManager.SessionStarted += _sessionManager_SessionStarted;
            _sessionManager.AuthenticationFailed += _sessionManager_AuthenticationFailed;
            _sessionManager.AuthenticationSucceeded += _sessionManager_AuthenticationSucceeded;
            _sessionManager.SessionEnded += _sessionManager_SessionEnded;

            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped += _sessionManager_PlaybackStopped;

            //_subManager.SubtitlesDownloaded += _subManager_SubtitlesDownloaded;
            _subManager.SubtitleDownloadFailure += _subManager_SubtitleDownloadFailure;

            _userManager.UserCreated += _userManager_UserCreated;
            _userManager.UserPasswordChanged += _userManager_UserPasswordChanged;
            _userManager.UserDeleted += _userManager_UserDeleted;
            _userManager.UserPolicyUpdated += _userManager_UserPolicyUpdated;
            _userManager.UserLockedOut += _userManager_UserLockedOut;

            //_config.ConfigurationUpdated += _config_ConfigurationUpdated;
            //_config.NamedConfigurationUpdated += _config_NamedConfigurationUpdated;

            _deviceManager.CameraImageUploaded += _deviceManager_CameraImageUploaded;

            _appHost.ApplicationUpdated += _appHost_ApplicationUpdated;
        }

        void _deviceManager_CameraImageUploaded(object sender, GenericEventArgs<CameraImageUploadInfo> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("CameraImageUploadedFrom"), e.Argument.Device.Name),
                Type = NotificationType.CameraImageUploaded.ToString()
            });
        }

        void _userManager_UserLockedOut(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserLockedOutWithName"), e.Argument.Name),
                Type = NotificationType.UserLockedOut.ToString(),
                UserId = e.Argument.Id
            });
        }

        void _subManager_SubtitleDownloadFailure(object sender, SubtitleDownloadFailureEventArgs e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("SubtitleDownloadFailureFromForItem"), e.Provider, Notifications.Notifications.GetItemName(e.Item)),
                Type = "SubtitleDownloadFailure",
                ItemId = e.Item.Id.ToString("N"),
                ShortOverview = e.Exception.Message
            });
        }

        void _sessionManager_PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                //_logger.LogWarning("PlaybackStopped reported with null media info.");
                return;
            }

            if (e.Item != null && e.Item.IsThemeMedia)
            {
                // Don't report theme song or local trailer playback
                return;
            }

            if (e.Users.Count == 0)
            {
                return;
            }

            var user = e.Users.First();

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserStoppedPlayingItemWithValues"), user.Name, GetItemName(item), e.DeviceName),
                Type = GetPlaybackStoppedNotificationType(item.MediaType),
                UserId = user.Id
            });
        }

        void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                //_logger.LogWarning("PlaybackStart reported with null media info.");
                return;
            }

            if (e.Item != null && e.Item.IsThemeMedia)
            {
                // Don't report theme song or local trailer playback
                return;
            }

            if (e.Users.Count == 0)
            {
                return;
            }

            var user = e.Users.First();

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserStartedPlayingItemWithValues"), user.Name, GetItemName(item), e.DeviceName),
                Type = GetPlaybackNotificationType(item.MediaType),
                UserId = user.Id
            });
        }

        private static string GetItemName(BaseItemDto item)
        {
            var name = item.Name;

            if (!string.IsNullOrEmpty(item.SeriesName))
            {
                name = item.SeriesName + " - " + name;
            }

            if (item.Artists != null && item.Artists.Length > 0)
            {
                name = item.Artists[0] + " - " + name;
            }

            return name;
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

        void _sessionManager_SessionEnded(object sender, SessionEventArgs e)
        {
            string name;
            var session = e.SessionInfo;

            if (string.IsNullOrEmpty(session.UserName))
            {
                name = string.Format(_localization.GetLocalizedString("DeviceOfflineWithName"), session.DeviceName);

                // Causing too much spam for now
                return;
            }
            else
            {
                name = string.Format(_localization.GetLocalizedString("UserOfflineFromDevice"), session.UserName, session.DeviceName);
            }

            CreateLogEntry(new ActivityLogEntry
            {
                Name = name,
                Type = "SessionEnded",
                ShortOverview = string.Format(_localization.GetLocalizedString("LabelIpAddressValue"), session.RemoteEndPoint),
                UserId = session.UserId
            });
        }

        void _sessionManager_AuthenticationSucceeded(object sender, GenericEventArgs<AuthenticationResult> e)
        {
            var user = e.Argument.User;

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("AuthenticationSucceededWithUserName"), user.Name),
                Type = "AuthenticationSucceeded",
                ShortOverview = string.Format(_localization.GetLocalizedString("LabelIpAddressValue"), e.Argument.SessionInfo.RemoteEndPoint),
                UserId = user.Id
            });
        }

        void _sessionManager_AuthenticationFailed(object sender, GenericEventArgs<AuthenticationRequest> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("FailedLoginAttemptWithUserName"), e.Argument.Username),
                Type = "AuthenticationFailed",
                ShortOverview = string.Format(_localization.GetLocalizedString("LabelIpAddressValue"), e.Argument.RemoteEndPoint),
                Severity = LogLevel.Error
            });
        }

        void _appHost_ApplicationUpdated(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("MessageApplicationUpdatedTo"), e.Argument.versionStr),
                Type = NotificationType.ApplicationUpdateInstalled.ToString(),
                Overview = e.Argument.description
            });
        }

        void _config_NamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("MessageNamedServerConfigurationUpdatedWithValue"), e.Key),
                Type = "NamedConfigurationUpdated"
            });
        }

        void _config_ConfigurationUpdated(object sender, EventArgs e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = _localization.GetLocalizedString("MessageServerConfigurationUpdated"),
                Type = "ServerConfigurationUpdated"
            });
        }

        void _userManager_UserPolicyUpdated(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserPolicyUpdatedWithName"), e.Argument.Name),
                Type = "UserPolicyUpdated",
                UserId = e.Argument.Id
            });
        }

        void _userManager_UserDeleted(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserDeletedWithName"), e.Argument.Name),
                Type = "UserDeleted"
            });
        }

        void _userManager_UserPasswordChanged(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserPasswordChangedWithName"), e.Argument.Name),
                Type = "UserPasswordChanged",
                UserId = e.Argument.Id
            });
        }

        void _userManager_UserCreated(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserCreatedWithName"), e.Argument.Name),
                Type = "UserCreated",
                UserId = e.Argument.Id
            });
        }

        void _subManager_SubtitlesDownloaded(object sender, SubtitleDownloadEventArgs e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("SubtitlesDownloadedForItem"), Notifications.Notifications.GetItemName(e.Item)),
                Type = "SubtitlesDownloaded",
                ItemId = e.Item.Id.ToString("N"),
                ShortOverview = string.Format(_localization.GetLocalizedString("ProviderValue"), e.Provider)
            });
        }

        void _sessionManager_SessionStarted(object sender, SessionEventArgs e)
        {
            string name;
            var session = e.SessionInfo;

            if (string.IsNullOrEmpty(session.UserName))
            {
                name = string.Format(_localization.GetLocalizedString("DeviceOnlineWithName"), session.DeviceName);

                // Causing too much spam for now
                return;
            }
            else
            {
                name = string.Format(_localization.GetLocalizedString("UserOnlineFromDevice"), session.UserName, session.DeviceName);
            }

            CreateLogEntry(new ActivityLogEntry
            {
                Name = name,
                Type = "SessionStarted",
                ShortOverview = string.Format(_localization.GetLocalizedString("LabelIpAddressValue"), session.RemoteEndPoint),
                UserId = session.UserId
            });
        }

        void _installationManager_PluginUpdated(object sender, GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("PluginUpdatedWithName"), e.Argument.Item1.Name),
                Type = NotificationType.PluginUpdateInstalled.ToString(),
                ShortOverview = string.Format(_localization.GetLocalizedString("VersionNumber"), e.Argument.Item2.versionStr),
                Overview = e.Argument.Item2.description
            });
        }

        void _installationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("PluginUninstalledWithName"), e.Argument.Name),
                Type = NotificationType.PluginUninstalled.ToString()
            });
        }

        void _installationManager_PluginInstalled(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("PluginInstalledWithName"), e.Argument.name),
                Type = NotificationType.PluginInstalled.ToString(),
                ShortOverview = string.Format(_localization.GetLocalizedString("VersionNumber"), e.Argument.versionStr)
            });
        }

        void _installationManager_PackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("NameInstallFailed"), installationInfo.Name),
                Type = NotificationType.InstallationFailed.ToString(),
                ShortOverview = string.Format(_localization.GetLocalizedString("VersionNumber"), installationInfo.Version),
                Overview = e.Exception.Message
            });
        }

        void _taskManager_TaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            var result = e.Result;
            var task = e.Task;

            var activityTask = task.ScheduledTask as IConfigurableScheduledTask;
            if (activityTask != null && !activityTask.IsLogged)
            {
                return;
            }

            var time = result.EndTimeUtc - result.StartTimeUtc;
            var runningTime = string.Format(_localization.GetLocalizedString("LabelRunningTimeValue"), ToUserFriendlyString(time));

            if (result.Status == TaskCompletionStatus.Failed)
            {
                var vals = new List<string>();

                if (!string.IsNullOrEmpty(e.Result.ErrorMessage))
                {
                    vals.Add(e.Result.ErrorMessage);
                }
                if (!string.IsNullOrEmpty(e.Result.LongErrorMessage))
                {
                    vals.Add(e.Result.LongErrorMessage);
                }

                CreateLogEntry(new ActivityLogEntry
                {
                    Name = string.Format(_localization.GetLocalizedString("ScheduledTaskFailedWithName"), task.Name),
                    Type = NotificationType.TaskFailed.ToString(),
                    Overview = string.Join(Environment.NewLine, vals.ToArray()),
                    ShortOverview = runningTime,
                    Severity = LogLevel.Error
                });
            }
        }

        private void CreateLogEntry(ActivityLogEntry entry)
        {
            try
            {
                _activityManager.Create(entry);
            }
            catch
            {
                // Logged at lower levels
            }
        }

        public void Dispose()
        {
            _taskManager.TaskCompleted -= _taskManager_TaskCompleted;

            _installationManager.PluginInstalled -= _installationManager_PluginInstalled;
            _installationManager.PluginUninstalled -= _installationManager_PluginUninstalled;
            _installationManager.PluginUpdated -= _installationManager_PluginUpdated;
            _installationManager.PackageInstallationFailed -= _installationManager_PackageInstallationFailed;

            _sessionManager.SessionStarted -= _sessionManager_SessionStarted;
            _sessionManager.AuthenticationFailed -= _sessionManager_AuthenticationFailed;
            _sessionManager.AuthenticationSucceeded -= _sessionManager_AuthenticationSucceeded;
            _sessionManager.SessionEnded -= _sessionManager_SessionEnded;

            _sessionManager.PlaybackStart -= _sessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped -= _sessionManager_PlaybackStopped;

            _subManager.SubtitlesDownloaded -= _subManager_SubtitlesDownloaded;
            _subManager.SubtitleDownloadFailure -= _subManager_SubtitleDownloadFailure;

            _userManager.UserCreated -= _userManager_UserCreated;
            _userManager.UserPasswordChanged -= _userManager_UserPasswordChanged;
            _userManager.UserDeleted -= _userManager_UserDeleted;
            _userManager.UserPolicyUpdated -= _userManager_UserPolicyUpdated;
            _userManager.UserLockedOut -= _userManager_UserLockedOut;

            _config.ConfigurationUpdated -= _config_ConfigurationUpdated;
            _config.NamedConfigurationUpdated -= _config_NamedConfigurationUpdated;

            _deviceManager.CameraImageUploaded -= _deviceManager_CameraImageUploaded;

            _appHost.ApplicationUpdated -= _appHost_ApplicationUpdated;
        }

        /// <summary>
        /// Constructs a user-friendly string for this TimeSpan instance.
        /// </summary>
        public static string ToUserFriendlyString(TimeSpan span)
        {
            const int DaysInYear = 365;
            const int DaysInMonth = 30;

            // Get each non-zero value from TimeSpan component
            List<string> values = new List<string>();

            // Number of years
            int days = span.Days;
            if (days >= DaysInYear)
            {
                int years = days / DaysInYear;
                values.Add(CreateValueString(years, "year"));
                days = days % DaysInYear;
            }
            // Number of months
            if (days >= DaysInMonth)
            {
                int months = days / DaysInMonth;
                values.Add(CreateValueString(months, "month"));
                days = days % DaysInMonth;
            }
            // Number of days
            if (days >= 1)
                values.Add(CreateValueString(days, "day"));
            // Number of hours
            if (span.Hours >= 1)
                values.Add(CreateValueString(span.Hours, "hour"));
            // Number of minutes
            if (span.Minutes >= 1)
                values.Add(CreateValueString(span.Minutes, "minute"));
            // Number of seconds (include when 0 if no other components included)
            if (span.Seconds >= 1 || values.Count == 0)
                values.Add(CreateValueString(span.Seconds, "second"));

            // Combine values into string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (builder.Length > 0)
                    builder.Append(i == values.Count - 1 ? " and " : ", ");
                builder.Append(values[i]);
            }
            // Return result
            return builder.ToString();
        }

        /// <summary>
        /// Constructs a string description of a time-span value.
        /// </summary>
        /// <param name="value">The value of this item</param>
        /// <param name="description">The name of this item (singular form)</param>
        private static string CreateValueString(int value, string description)
        {
            return String.Format("{0:#,##0} {1}",
                value, value == 1 ? description : String.Format("{0}s", description));
        }
    }
}
