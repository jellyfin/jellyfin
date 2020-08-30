using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events.System;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Server.Implementations.Events.Consumers.System
{
    /// <summary>
    /// Notifies users when there is an update available.
    /// </summary>
    public class UpdateAvailableNotifier : IEventConsumer<UpdateAvailableEventArgs>
    {
        private readonly IServerApplicationHost _applicationHost;
        private readonly INotificationManager _notificationManager;
        private readonly ILocalizationManager _localizationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateAvailableNotifier"/> class.
        /// </summary>
        /// <param name="applicationHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="notificationManager">Instance of the <see cref="INotificationManager"/> interface.</param>
        /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        public UpdateAvailableNotifier(
            IServerApplicationHost applicationHost,
            INotificationManager notificationManager,
            ILocalizationManager localizationManager)
        {
            _applicationHost = applicationHost;
            _notificationManager = notificationManager;
            _localizationManager = localizationManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(UpdateAvailableEventArgs eventArgs)
        {
            if (!_applicationHost.HasUpdateAvailable)
            {
                return;
            }

            var notification = new NotificationRequest
            {
                Description = "Please see jellyfin.org for details.",
                NotificationType = NotificationType.ApplicationUpdateAvailable.ToString(),
                Name = _localizationManager.GetLocalizedString("NewVersionIsAvailable")
            };

            await _notificationManager.SendNotification(notification, null, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
