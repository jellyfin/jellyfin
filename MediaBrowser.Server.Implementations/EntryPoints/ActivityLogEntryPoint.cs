using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Activity;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class ActivityLogEntryPoint : IServerEntryPoint
    {
        private readonly IInstallationManager _installationManager;

        //private readonly ILogManager _logManager;
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

        public ActivityLogEntryPoint(ISessionManager sessionManager, ITaskManager taskManager, IActivityManager activityManager, ILocalizationManager localization, IInstallationManager installationManager, ILibraryManager libraryManager, ISubtitleManager subManager, IUserManager userManager, IServerConfigurationManager config, IServerApplicationHost appHost)
        {
            //_logger = _logManager.GetLogger("ActivityLogEntryPoint");
            _sessionManager = sessionManager;
            _taskManager = taskManager;
            _activityManager = activityManager;
            _localization = localization;
            _installationManager = installationManager;
            _libraryManager = libraryManager;
            _subManager = subManager;
            _userManager = userManager;
            _config = config;
            //_logManager = logManager;
            _appHost = appHost;
        }

        public void Run()
        {
            //_taskManager.TaskExecuting += _taskManager_TaskExecuting;
            //_taskManager.TaskCompleted += _taskManager_TaskCompleted;

            //_installationManager.PluginInstalled += _installationManager_PluginInstalled;
            //_installationManager.PluginUninstalled += _installationManager_PluginUninstalled;
            //_installationManager.PluginUpdated += _installationManager_PluginUpdated;

            //_libraryManager.ItemAdded += _libraryManager_ItemAdded;
            //_libraryManager.ItemRemoved += _libraryManager_ItemRemoved;

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
            _userManager.UserConfigurationUpdated += _userManager_UserConfigurationUpdated;
            _userManager.UserLockedOut += _userManager_UserLockedOut;

            //_config.ConfigurationUpdated += _config_ConfigurationUpdated;
            //_config.NamedConfigurationUpdated += _config_NamedConfigurationUpdated;

            //_logManager.LoggerLoaded += _logManager_LoggerLoaded;

            _appHost.ApplicationUpdated += _appHost_ApplicationUpdated;
        }

        void _userManager_UserLockedOut(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserLockedOutWithName"), e.Argument.Name),
                Type = "UserLockedOut",
                UserId = e.Argument.Id.ToString("N")
            });
        }

        void _subManager_SubtitleDownloadFailure(object sender, SubtitleDownloadFailureEventArgs e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("SubtitleDownloadFailureForItem"), Notifications.Notifications.GetItemName(e.Item)),
                Type = "SubtitleDownloadFailure",
                ItemId = e.Item.Id.ToString("N"),
                ShortOverview = string.Format(_localization.GetLocalizedString("ProviderValue"), e.Provider),
                Overview = LogHelper.GetLogMessage(e.Exception).ToString()
            });
        }

        void _sessionManager_PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                //_logger.Warn("PlaybackStopped reported with null media info.");
                return;
            }

            var themeMedia = item as IThemeMedia;
            if (themeMedia != null && themeMedia.IsThemeMedia)
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
                Name = string.Format(_localization.GetLocalizedString("UserStoppedPlayingItemWithValues"), user.Name, item.Name),
                Type = "PlaybackStopped",
                ShortOverview = string.Format(_localization.GetLocalizedString("AppDeviceValues"), e.ClientName, e.DeviceName),
                UserId = user.Id.ToString("N")
            });
        }

        void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var item = e.MediaInfo;

            if (item == null)
            {
                //_logger.Warn("PlaybackStart reported with null media info.");
                return;
            }

            var themeMedia = item as IThemeMedia;
            if (themeMedia != null && themeMedia.IsThemeMedia)
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
                Name = string.Format(_localization.GetLocalizedString("UserStartedPlayingItemWithValues"), user.Name, item.Name),
                Type = "PlaybackStart",
                ShortOverview = string.Format(_localization.GetLocalizedString("AppDeviceValues"), e.ClientName, e.DeviceName),
                UserId = user.Id.ToString("N")
            });
        }

        void _sessionManager_SessionEnded(object sender, SessionEventArgs e)
        {
            string name;
            var session = e.SessionInfo;

            if (string.IsNullOrWhiteSpace(session.UserName))
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
                UserId = session.UserId.HasValue ? session.UserId.Value.ToString("N") : null
            });
        }

        void _sessionManager_AuthenticationSucceeded(object sender, GenericEventArgs<AuthenticationRequest> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("AuthenticationSucceededWithUserName"), e.Argument.Username),
                Type = "AuthenticationSucceeded",
                ShortOverview = string.Format(_localization.GetLocalizedString("LabelIpAddressValue"), e.Argument.RemoteEndPoint)
            });
        }

        void _sessionManager_AuthenticationFailed(object sender, GenericEventArgs<AuthenticationRequest> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("FailedLoginAttemptWithUserName"), e.Argument.Username),
                Type = "AuthenticationFailed",
                ShortOverview = string.Format(_localization.GetLocalizedString("LabelIpAddressValue"), e.Argument.RemoteEndPoint),
                Severity = LogSeverity.Error
            });
        }

        void _appHost_ApplicationUpdated(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = _localization.GetLocalizedString("MessageApplicationUpdated"),
                Type = "ApplicationUpdated",
                ShortOverview = string.Format(_localization.GetLocalizedString("VersionNumber"), e.Argument.versionStr),
                Overview = e.Argument.description
            });
        }

        void _logManager_LoggerLoaded(object sender, EventArgs e)
        {
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

        void _userManager_UserConfigurationUpdated(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserConfigurationUpdatedWithName"), e.Argument.Name),
                Type = "UserConfigurationUpdated",
                UserId = e.Argument.Id.ToString("N")
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
                UserId = e.Argument.Id.ToString("N")
            });
        }

        void _userManager_UserCreated(object sender, GenericEventArgs<User> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("UserCreatedWithName"), e.Argument.Name),
                Type = "UserCreated",
                UserId = e.Argument.Id.ToString("N")
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

            if (string.IsNullOrWhiteSpace(session.UserName))
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
                UserId = session.UserId.HasValue ? session.UserId.Value.ToString("N") : null
            });
        }

        void _libraryManager_ItemRemoved(object sender, ItemChangeEventArgs e)
        {
            if (e.Item.SourceType != SourceType.Library)
            {
                return;
            }

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("ItemRemovedWithName"), Notifications.Notifications.GetItemName(e.Item)),
                Type = "ItemRemoved"
            });
        }

        void _libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (e.Item.SourceType != SourceType.Library)
            {
                return;
            }

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("ItemAddedWithName"), Notifications.Notifications.GetItemName(e.Item)),
                Type = "ItemAdded",
                ItemId = e.Item.Id.ToString("N")
            });
        }

        void _installationManager_PluginUpdated(object sender, GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("PluginUpdatedWithName"), e.Argument.Item1.Name),
                Type = "PluginUpdated",
                ShortOverview = string.Format(_localization.GetLocalizedString("VersionNumber"), e.Argument.Item2.versionStr),
                Overview = e.Argument.Item2.description
            });
        }

        void _installationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("PluginUninstalledWithName"), e.Argument.Name),
                Type = "PluginUninstalled"
            });
        }

        void _installationManager_PluginInstalled(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("PluginInstalledWithName"), e.Argument.name),
                Type = "PluginInstalled",
                ShortOverview = string.Format(_localization.GetLocalizedString("VersionNumber"), e.Argument.versionStr)
            });
        }

        void _taskManager_TaskExecuting(object sender, GenericEventArgs<IScheduledTaskWorker> e)
        {
            var task = e.Argument;

            var activityTask = task.ScheduledTask as IScheduledTaskActivityLog;
            if (activityTask != null && !activityTask.IsActivityLogged)
            {
                return;
            }

            CreateLogEntry(new ActivityLogEntry
            {
                Name = string.Format(_localization.GetLocalizedString("ScheduledTaskStartedWithName"), task.Name),
                Type = "ScheduledTaskStarted"
            });
        }

        void _taskManager_TaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            var result = e.Result;
            var task = e.Task;

            var activityTask = task.ScheduledTask as IScheduledTaskActivityLog;
            if (activityTask != null && !activityTask.IsActivityLogged)
            {
                return;
            }
            
            var time = result.EndTimeUtc - result.StartTimeUtc;
            var runningTime = string.Format(_localization.GetLocalizedString("LabelRunningTimeValue"), ToUserFriendlyString(time));

            if (result.Status == TaskCompletionStatus.Failed)
            {
                var vals = new List<string>();

                if (!string.IsNullOrWhiteSpace(e.Result.ErrorMessage))
                {
                    vals.Add(e.Result.ErrorMessage);
                }
                if (!string.IsNullOrWhiteSpace(e.Result.LongErrorMessage))
                {
                    vals.Add(e.Result.LongErrorMessage);
                }

                CreateLogEntry(new ActivityLogEntry
                {
                    Name = string.Format(_localization.GetLocalizedString("ScheduledTaskFailedWithName"), task.Name),
                    Type = "ScheduledTaskFailed",
                    Overview = string.Join(Environment.NewLine, vals.ToArray()),
                    ShortOverview = runningTime,
                    Severity = LogSeverity.Error
                });
            }
        }

        private async void CreateLogEntry(ActivityLogEntry entry)
        {
            try
            {
                await _activityManager.Create(entry).ConfigureAwait(false);
            }
            catch
            {
                // Logged at lower levels
            }
        }

        public void Dispose()
        {
            _taskManager.TaskExecuting -= _taskManager_TaskExecuting;
            _taskManager.TaskCompleted -= _taskManager_TaskCompleted;

            _installationManager.PluginInstalled -= _installationManager_PluginInstalled;
            _installationManager.PluginUninstalled -= _installationManager_PluginUninstalled;
            _installationManager.PluginUpdated -= _installationManager_PluginUpdated;

            _libraryManager.ItemAdded -= _libraryManager_ItemAdded;
            _libraryManager.ItemRemoved -= _libraryManager_ItemRemoved;

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
            _userManager.UserConfigurationUpdated -= _userManager_UserConfigurationUpdated;
            _userManager.UserLockedOut -= _userManager_UserLockedOut;

            _config.ConfigurationUpdated -= _config_ConfigurationUpdated;
            _config.NamedConfigurationUpdated -= _config_NamedConfigurationUpdated;

            //_logManager.LoggerLoaded -= _logManager_LoggerLoaded;

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
