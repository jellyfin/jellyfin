#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Activity
{
    public sealed class ActivityLogEntryPoint : IServerEntryPoint
    {
        private readonly ILogger _logger;
        private readonly IInstallationManager _installationManager;
        private readonly ISessionManager _sessionManager;
        private readonly ITaskManager _taskManager;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;
        private readonly ISubtitleManager _subManager;
        private readonly IUserManager _userManager;
        private readonly IDeviceManager _deviceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogEntryPoint"/> class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="sessionManager"></param>
        /// <param name="deviceManager"></param>
        /// <param name="taskManager"></param>
        /// <param name="activityManager"></param>
        /// <param name="localization"></param>
        /// <param name="installationManager"></param>
        /// <param name="subManager"></param>
        /// <param name="userManager"></param>
        /// <param name="appHost"></param>
        public ActivityLogEntryPoint(
            ILogger<ActivityLogEntryPoint> logger,
            ISessionManager sessionManager,
            IDeviceManager deviceManager,
            ITaskManager taskManager,
            IActivityManager activityManager,
            ILocalizationManager localization,
            IInstallationManager installationManager,
            ISubtitleManager subManager,
            IUserManager userManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _deviceManager = deviceManager;
            _taskManager = taskManager;
            _activityManager = activityManager;
            _localization = localization;
            _installationManager = installationManager;
            _subManager = subManager;
            _userManager = userManager;
        }

        public Task RunAsync()
        {
            _taskManager.TaskCompleted += OnTaskCompleted;

            _installationManager.PluginInstalled += OnPluginInstalled;
            _installationManager.PluginUninstalled += OnPluginUninstalled;
            _installationManager.PluginUpdated += OnPluginUpdated;
            _installationManager.PackageInstallationFailed += OnPackageInstallationFailed;

            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.AuthenticationFailed += OnAuthenticationFailed;
            _sessionManager.AuthenticationSucceeded += OnAuthenticationSucceeded;
            _sessionManager.SessionEnded += OnSessionEnded;
            _sessionManager.PlaybackStart += OnPlaybackStart;
            _sessionManager.PlaybackStopped += OnPlaybackStopped;

            _subManager.SubtitleDownloadFailure += OnSubtitleDownloadFailure;

            _userManager.UserCreated += OnUserCreated;
            _userManager.UserPasswordChanged += OnUserPasswordChanged;
            _userManager.UserDeleted += OnUserDeleted;
            _userManager.UserPolicyUpdated += OnUserPolicyUpdated;
            _userManager.UserLockedOut += OnUserLockedOut;

            _deviceManager.CameraImageUploaded += OnCameraImageUploaded;

            return Task.CompletedTask;
        }

        private void OnCameraImageUploaded(object sender, GenericEventArgs<CameraImageUploadInfo> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("CameraImageUploadedFrom"),
                    e.Argument.Device.Name),
                Type = NotificationType.CameraImageUploaded.ToString()
            });
        }

        private void OnUserLockedOut(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserLockedOutWithName"),
                    e.Argument.Name),
                Type = NotificationType.UserLockedOut.ToString(),
                UserId = e.Argument.Id
            });
        }

        private void OnSubtitleDownloadFailure(object sender, SubtitleDownloadFailureEventArgs e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("SubtitleDownloadFailureFromForItem"),
                    e.Provider,
                    Emby.Notifications.NotificationEntryPoint.GetItemName(e.Item)),
                Type = "SubtitleDownloadFailure",
                ItemId = e.Item.Id.ToString("N", CultureInfo.InvariantCulture),
                ShortOverview = e.Exception.Message
            });
        }

        private void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                _logger.LogWarning("PlaybackStopped reported with null media info.");
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

            var user = e.Users[0];

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserStoppedPlayingItemWithValues"), user.Name, GetItemName(item), e.DeviceName),
                Type = GetPlaybackStoppedNotificationType(item.MediaType),
                UserId = user.Id
            });
        }

        private void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                _logger.LogWarning("PlaybackStart reported with null media info.");
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
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserStartedPlayingItemWithValues"),
                    user.Name,
                    GetItemName(item),
                    e.DeviceName),
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

            if (item.Artists != null && item.Artists.Count > 0)
            {
                name = item.Artists[0] + " - " + name;
            }

            return name;
        }

        private static string GetPlaybackNotificationType(string mediaType)
        {
            if (string.Equals(mediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.AudioPlayback.ToString();
            }

            if (string.Equals(mediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.VideoPlayback.ToString();
            }

            return null;
        }

        private static string GetPlaybackStoppedNotificationType(string mediaType)
        {
            if (string.Equals(mediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.AudioPlaybackStopped.ToString();
            }

            if (string.Equals(mediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                return NotificationType.VideoPlaybackStopped.ToString();
            }

            return null;
        }

        private void OnSessionEnded(object sender, SessionEventArgs e)
        {
            string name;
            var session = e.SessionInfo;

            if (string.IsNullOrEmpty(session.UserName))
            {
                name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("DeviceOfflineWithName"),
                    session.DeviceName);

                // Causing too much spam for now
                return;
            }
            else
            {
                name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserOfflineFromDevice"),
                    session.UserName,
                    session.DeviceName);
            }

            CreateLogEntry(new ActivityLogEntry
            {
                Name = name,
                Type = "SessionEnded",
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    session.RemoteEndPoint),
                UserId = session.UserId
            });
        }

        private void OnAuthenticationSucceeded(object sender, GenericEventArgs<AuthenticationResult> e)
        {
            var user = e.Argument.User;

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("AuthenticationSucceededWithUserName"),
                    user.Name),
                Type = "AuthenticationSucceeded",
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    e.Argument.SessionInfo.RemoteEndPoint),
                UserId = user.Id
            });
        }

        private void OnAuthenticationFailed(object sender, GenericEventArgs<AuthenticationRequest> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("FailedLoginAttemptWithUserName"),
                    e.Argument.Username),
                Type = "AuthenticationFailed",
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    e.Argument.RemoteEndPoint),
                Severity = LogLevel.Error
            });
        }

        private void OnUserPolicyUpdated(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserPolicyUpdatedWithName"),
                    e.Argument.Name),
                Type = "UserPolicyUpdated",
                UserId = e.Argument.Id
            });
        }

        private void OnUserDeleted(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserDeletedWithName"),
                    e.Argument.Name),
                Type = "UserDeleted"
            });
        }

        private void OnUserPasswordChanged(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserPasswordChangedWithName"),
                    e.Argument.Name),
                Type = "UserPasswordChanged",
                UserId = e.Argument.Id
            });
        }

        private void OnUserCreated(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserCreatedWithName"),
                    e.Argument.Name),
                Type = "UserCreated",
                UserId = e.Argument.Id
            });
        }

        private void OnSessionStarted(object sender, SessionEventArgs e)
        {
            string name;
            var session = e.SessionInfo;

            if (string.IsNullOrEmpty(session.UserName))
            {
                name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("DeviceOnlineWithName"),
                    session.DeviceName);

                // Causing too much spam for now
                return;
            }
            else
            {
                name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserOnlineFromDevice"),
                    session.UserName,
                    session.DeviceName);
            }

            CreateLogEntry(new ActivityLogEntry
            {
                Name = name,
                Type = "SessionStarted",
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    session.RemoteEndPoint),
                UserId = session.UserId
            });
        }

        private void OnPluginUpdated(object sender, GenericEventArgs<(IPlugin, PackageVersionInfo)> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("PluginUpdatedWithName"),
                    e.Argument.Item1.Name),
                Type = NotificationType.PluginUpdateInstalled.ToString(),
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("VersionNumber"),
                    e.Argument.Item2.versionStr),
                Overview = e.Argument.Item2.description
            });
        }

        private void OnPluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("PluginUninstalledWithName"),
                    e.Argument.Name),
                Type = NotificationType.PluginUninstalled.ToString()
            });
        }

        private void OnPluginInstalled(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("PluginInstalledWithName"),
                    e.Argument.name),
                Type = NotificationType.PluginInstalled.ToString(),
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("VersionNumber"),
                    e.Argument.versionStr)
            });
        }

        private void OnPackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("NameInstallFailed"),
                    installationInfo.Name),
                Type = NotificationType.InstallationFailed.ToString(),
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("VersionNumber"),
                    installationInfo.Version),
                Overview = e.Exception.Message
            });
        }

        private void OnTaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            var result = e.Result;
            var task = e.Task;

            var activityTask = task.ScheduledTask as IConfigurableScheduledTask;
            if (activityTask != null && !activityTask.IsLogged)
            {
                return;
            }

            var time = result.EndTimeUtc - result.StartTimeUtc;
            var runningTime = string.Format(
                CultureInfo.InvariantCulture,
                _localization.GetLocalizedString("LabelRunningTimeValue"),
                ToUserFriendlyString(time));

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
                    Name = string.Format(
                        CultureInfo.InvariantCulture,
                        _localization.GetLocalizedString("ScheduledTaskFailedWithName"),
                        task.Name),
                    Type = NotificationType.TaskFailed.ToString(),
                    Overview = string.Join(Environment.NewLine, vals),
                    ShortOverview = runningTime,
                    Severity = LogLevel.Error
                });
            }
        }

        private void CreateLogEntry(ActivityLogEntry entry)
            => _activityManager.Create(entry);

        /// <inheritdoc />
        public void Dispose()
        {
            _taskManager.TaskCompleted -= OnTaskCompleted;

            _installationManager.PluginInstalled -= OnPluginInstalled;
            _installationManager.PluginUninstalled -= OnPluginUninstalled;
            _installationManager.PluginUpdated -= OnPluginUpdated;
            _installationManager.PackageInstallationFailed -= OnPackageInstallationFailed;

            _sessionManager.SessionStarted -= OnSessionStarted;
            _sessionManager.AuthenticationFailed -= OnAuthenticationFailed;
            _sessionManager.AuthenticationSucceeded -= OnAuthenticationSucceeded;
            _sessionManager.SessionEnded -= OnSessionEnded;

            _sessionManager.PlaybackStart -= OnPlaybackStart;
            _sessionManager.PlaybackStopped -= OnPlaybackStopped;

            _subManager.SubtitleDownloadFailure -= OnSubtitleDownloadFailure;

            _userManager.UserCreated -= OnUserCreated;
            _userManager.UserPasswordChanged -= OnUserPasswordChanged;
            _userManager.UserDeleted -= OnUserDeleted;
            _userManager.UserPolicyUpdated -= OnUserPolicyUpdated;
            _userManager.UserLockedOut -= OnUserLockedOut;

            _deviceManager.CameraImageUploaded -= OnCameraImageUploaded;
        }

        /// <summary>
        /// Constructs a user-friendly string for this TimeSpan instance.
        /// </summary>
        public static string ToUserFriendlyString(TimeSpan span)
        {
            const int DaysInYear = 365;
            const int DaysInMonth = 30;

            // Get each non-zero value from TimeSpan component
            var values = new List<string>();

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
            {
                values.Add(CreateValueString(days, "day"));
            }

            // Number of hours
            if (span.Hours >= 1)
            {
                values.Add(CreateValueString(span.Hours, "hour"));
            }

            // Number of minutes
            if (span.Minutes >= 1)
            {
                values.Add(CreateValueString(span.Minutes, "minute"));
            }

            // Number of seconds (include when 0 if no other components included)
            if (span.Seconds >= 1 || values.Count == 0)
            {
                values.Add(CreateValueString(span.Seconds, "second"));
            }

            // Combine values into string
            var builder = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(i == values.Count - 1 ? " and " : ", ");
                }

                builder.Append(values[i]);
            }

            // Return result
            return builder.ToString();
        }

        /// <summary>
        /// Constructs a string description of a time-span value.
        /// </summary>
        /// <param name="value">The value of this item.</param>
        /// <param name="description">The name of this item (singular form).</param>
        private static string CreateValueString(int value, string description)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:#,##0} {1}",
                value,
                value == 1 ? description : string.Format(CultureInfo.InvariantCulture, "{0}s", description));
        }
    }
}
