using System;
using System.Globalization;
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
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Activity
{
    /// <summary>
    /// Entry point for the activity logger.
    /// </summary>
    public sealed class ActivityLogEntryPoint : IServerEntryPoint
    {
        private readonly IInstallationManager _installationManager;
        private readonly ISessionManager _sessionManager;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;
        private readonly ISubtitleManager _subManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogEntryPoint"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        /// <param name="localization">The localization manager.</param>
        /// <param name="installationManager">The installation manager.</param>
        /// <param name="subManager">The subtitle manager.</param>
        /// <param name="userManager">The user manager.</param>
        public ActivityLogEntryPoint(
            ISessionManager sessionManager,
            IActivityManager activityManager,
            ILocalizationManager localization,
            IInstallationManager installationManager,
            ISubtitleManager subManager,
            IUserManager userManager)
        {
            _sessionManager = sessionManager;
            _activityManager = activityManager;
            _localization = localization;
            _installationManager = installationManager;
            _subManager = subManager;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _installationManager.PluginInstalled += OnPluginInstalled;
            _installationManager.PluginUninstalled += OnPluginUninstalled;
            _installationManager.PluginUpdated += OnPluginUpdated;
            _installationManager.PackageInstallationFailed += OnPackageInstallationFailed;

            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.AuthenticationFailed += OnAuthenticationFailed;
            _sessionManager.AuthenticationSucceeded += OnAuthenticationSucceeded;
            _sessionManager.SessionEnded += OnSessionEnded;

            _subManager.SubtitleDownloadFailure += OnSubtitleDownloadFailure;

            _userManager.OnUserCreated += OnUserCreated;
            _userManager.OnUserPasswordChanged += OnUserPasswordChanged;
            _userManager.OnUserDeleted += OnUserDeleted;
            _userManager.OnUserLockedOut += OnUserLockedOut;

            return Task.CompletedTask;
        }

        private async void OnUserLockedOut(object sender, GenericEventArgs<User> e)
        {
            await CreateLogEntry(new ActivityLog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        _localization.GetLocalizedString("UserLockedOutWithName"),
                        e.Argument.Username),
                    NotificationType.UserLockedOut.ToString(),
                    e.Argument.Id)
            {
                LogSeverity = LogLevel.Error
            }).ConfigureAwait(false);
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

        private async void OnUserDeleted(object sender, GenericEventArgs<User> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserDeletedWithName"),
                    e.Argument.Username),
                "UserDeleted",
                Guid.Empty))
                .ConfigureAwait(false);
        }

        private async void OnUserPasswordChanged(object sender, GenericEventArgs<User> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserPasswordChangedWithName"),
                    e.Argument.Username),
                "UserPasswordChanged",
                e.Argument.Id))
                .ConfigureAwait(false);
        }

        private async void OnUserCreated(object sender, GenericEventArgs<User> e)
        {
            await CreateLogEntry(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserCreatedWithName"),
                    e.Argument.Username),
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

        private async Task CreateLogEntry(ActivityLog entry)
            => await _activityManager.CreateAsync(entry).ConfigureAwait(false);

        /// <inheritdoc />
        public void Dispose()
        {
            _installationManager.PluginInstalled -= OnPluginInstalled;
            _installationManager.PluginUninstalled -= OnPluginUninstalled;
            _installationManager.PluginUpdated -= OnPluginUpdated;
            _installationManager.PackageInstallationFailed -= OnPackageInstallationFailed;

            _sessionManager.SessionStarted -= OnSessionStarted;
            _sessionManager.AuthenticationFailed -= OnAuthenticationFailed;
            _sessionManager.AuthenticationSucceeded -= OnAuthenticationSucceeded;
            _sessionManager.SessionEnded -= OnSessionEnded;

            _subManager.SubtitleDownloadFailure -= OnSubtitleDownloadFailure;

            _userManager.OnUserCreated -= OnUserCreated;
            _userManager.OnUserPasswordChanged -= OnUserPasswordChanged;
            _userManager.OnUserDeleted -= OnUserDeleted;
            _userManager.OnUserLockedOut -= OnUserLockedOut;
        }
    }
}
