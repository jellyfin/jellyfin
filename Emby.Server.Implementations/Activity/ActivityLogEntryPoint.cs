using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Authentication;
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
    /// <summary>
    /// Entry point for the activity logger.
    /// </summary>
    public sealed class ActivityLogEntryPoint : IServerEntryPoint
    {
        private readonly ILogger<ActivityLogEntryPoint> _logger;
        private readonly IInstallationManager _installationManager;
        private readonly ISessionManager _sessionManager;
        private readonly ITaskManager _taskManager;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;
        private readonly ISubtitleManager _subManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogEntryPoint"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        /// <param name="localization">The localization manager.</param>
        /// <param name="installationManager">The installation manager.</param>
        /// <param name="subManager">The subtitle manager.</param>
        /// <param name="userManager">The user manager.</param>
        public ActivityLogEntryPoint(
            ILogger<ActivityLogEntryPoint> logger,
            ISessionManager sessionManager,
            ITaskManager taskManager,
            IActivityManager activityManager,
            ILocalizationManager localization,
            IInstallationManager installationManager,
            ISubtitleManager subManager,
            IUserManager userManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _taskManager = taskManager;
            _activityManager = activityManager;
            _localization = localization;
            _installationManager = installationManager;
            _subManager = subManager;
            _userManager = userManager;
        }

        /// <inheritdoc />
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

            return Task.CompletedTask;
        }

        private async void OnUserLockedOut(object sender, GenericEventArgs<MediaBrowser.Controller.Entities.User> e)
        {
            await CreateLogEntry(new ActivityLog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        _localization.GetLocalizedString("UserLockedOutWithName"),
                        e.Argument.Name),
                    NotificationType.UserLockedOut.ToString(),
                    e.Argument.Id))
                .ConfigureAwait(false);
        }

        private async void OnSubtitleDownloadFailure(object sender, SubtitleDownloadFailureEventArgs e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("SubtitleDownloadFailureFromForItem"),
                    e.Provider,
                    Notifications.NotificationEntryPoint.GetItemName(e.Item)),
                "SubtitleDownloadFailure",
                Guid.Empty)
            {
                ItemId = e.Item.Id.ToString("N", CultureInfo.InvariantCulture),
                ShortOverview = e.Exception.Message
            }).ConfigureAwait(false);
        }

        private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
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

            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserStoppedPlayingItemWithValues"),
                    user.Name,
                    GetItemName(item),
                    e.DeviceName),
                GetPlaybackStoppedNotificationType(item.MediaType),
                user.Id))
                .ConfigureAwait(false);
        }

        private async void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
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

            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserStartedPlayingItemWithValues"),
                    user.Name,
                    GetItemName(item),
                    e.DeviceName),
                GetPlaybackNotificationType(item.MediaType),
                user.Id))
                .ConfigureAwait(false);
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

        private async void OnSessionEnded(object sender, SessionEventArgs e)
        {
            var session = e.SessionInfo;

            if (string.IsNullOrEmpty(session.UserName))
            {
                return;
            }

            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserOfflineFromDevice"),
                    session.UserName,
                    session.DeviceName),
                "SessionEnded",
                session.UserId)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    session.RemoteEndPoint),
            }).ConfigureAwait(false);
        }

        private async void OnAuthenticationSucceeded(object sender, GenericEventArgs<AuthenticationResult> e)
        {
            var user = e.Argument.User;

            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("AuthenticationSucceededWithUserName"),
                    user.Name),
                "AuthenticationSucceeded",
                user.Id)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    e.Argument.SessionInfo.RemoteEndPoint),
            }).ConfigureAwait(false);
        }

        private async void OnAuthenticationFailed(object sender, GenericEventArgs<AuthenticationRequest> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("FailedLoginAttemptWithUserName"),
                    e.Argument.Username),
                "AuthenticationFailed",
                Guid.Empty)
            {
                LogSeverity = LogLevel.Error,
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    e.Argument.RemoteEndPoint),
            }).ConfigureAwait(false);
        }

        private async void OnUserPolicyUpdated(object sender, GenericEventArgs<MediaBrowser.Controller.Entities.User> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserPolicyUpdatedWithName"),
                    e.Argument.Name),
                "UserPolicyUpdated",
                e.Argument.Id))
                .ConfigureAwait(false);
        }

        private async void OnUserDeleted(object sender, GenericEventArgs<MediaBrowser.Controller.Entities.User> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserDeletedWithName"),
                    e.Argument.Name),
                "UserDeleted",
                Guid.Empty))
                .ConfigureAwait(false);
        }

        private async void OnUserPasswordChanged(object sender, GenericEventArgs<MediaBrowser.Controller.Entities.User> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserPasswordChangedWithName"),
                    e.Argument.Name),
                "UserPasswordChanged",
                e.Argument.Id))
                .ConfigureAwait(false);
        }

        private async void OnUserCreated(object sender, GenericEventArgs<MediaBrowser.Controller.Entities.User> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserCreatedWithName"),
                    e.Argument.Name),
                "UserCreated",
                e.Argument.Id))
                .ConfigureAwait(false);
        }

        private async void OnSessionStarted(object sender, SessionEventArgs e)
        {
            var session = e.SessionInfo;

            if (string.IsNullOrEmpty(session.UserName))
            {
                return;
            }

            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserOnlineFromDevice"),
                    session.UserName,
                    session.DeviceName),
                "SessionStarted",
                session.UserId)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("LabelIpAddressValue"),
                    session.RemoteEndPoint)
            }).ConfigureAwait(false);
        }

        private async void OnPluginUpdated(object sender, InstallationInfo e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("PluginUpdatedWithName"),
                    e.Name),
                NotificationType.PluginUpdateInstalled.ToString(),
                Guid.Empty)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("VersionNumber"),
                    e.Version),
                Overview = e.Changelog
            }).ConfigureAwait(false);
        }

        private async void OnPluginUninstalled(object sender, IPlugin e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("PluginUninstalledWithName"),
                    e.Name),
                NotificationType.PluginUninstalled.ToString(),
                Guid.Empty))
                .ConfigureAwait(false);
        }

        private async void OnPluginInstalled(object sender, InstallationInfo e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("PluginInstalledWithName"),
                    e.Name),
                NotificationType.PluginInstalled.ToString(),
                Guid.Empty)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("VersionNumber"),
                    e.Version)
            }).ConfigureAwait(false);
        }

        private async void OnPackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("NameInstallFailed"),
                    installationInfo.Name),
                NotificationType.InstallationFailed.ToString(),
                Guid.Empty)
            {
                ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("VersionNumber"),
                    installationInfo.Version),
                Overview = e.Exception.Message
            }).ConfigureAwait(false);
        }

        private async void OnTaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            var result = e.Result;
            var task = e.Task;

            if (task.ScheduledTask is IConfigurableScheduledTask activityTask
                && !activityTask.IsLogged)
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

                await CreateLogEntry(new ActivityLog(
                    string.Format(CultureInfo.InvariantCulture, _localization.GetLocalizedString("ScheduledTaskFailedWithName"), task.Name),
                    NotificationType.TaskFailed.ToString(),
                    Guid.Empty)
                {
                    LogSeverity = LogLevel.Error,
                    Overview = string.Join(Environment.NewLine, vals),
                    ShortOverview = runningTime
                }).ConfigureAwait(false);
            }
        }

        private async Task CreateLogEntry(ActivityLog entry)
            => await _activityManager.CreateAsync(entry).ConfigureAwait(false);

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
        }

        /// <summary>
        /// Constructs a user-friendly string for this TimeSpan instance.
        /// </summary>
        private static string ToUserFriendlyString(TimeSpan span)
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
                days %= DaysInYear;
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
