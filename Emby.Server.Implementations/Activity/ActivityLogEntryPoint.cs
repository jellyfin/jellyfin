using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Updates;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogEntryPoint"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        /// <param name="localization">The localization manager.</param>
        /// <param name="installationManager">The installation manager.</param>
        public ActivityLogEntryPoint(
            ISessionManager sessionManager,
            IActivityManager activityManager,
            ILocalizationManager localization,
            IInstallationManager installationManager)
        {
            _sessionManager = sessionManager;
            _activityManager = activityManager;
            _localization = localization;
            _installationManager = installationManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _installationManager.PluginUpdated += OnPluginUpdated;
            _installationManager.PackageInstallationFailed += OnPackageInstallationFailed;

            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.SessionEnded += OnSessionEnded;

            return Task.CompletedTask;
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
            _installationManager.PluginUpdated -= OnPluginUpdated;
            _installationManager.PackageInstallationFailed -= OnPackageInstallationFailed;

            _sessionManager.SessionStarted -= OnSessionStarted;
            _sessionManager.SessionEnded -= OnSessionEnded;
        }
    }
}
