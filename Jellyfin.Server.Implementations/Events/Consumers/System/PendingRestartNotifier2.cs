using System.Globalization;
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
    /// Notifies users when there is a pending restart.
    /// </summary>
    public class PendingRestartNotifier2 : IEventConsumer<PendingRestartEventArgs>
    {
        private readonly IServerApplicationHost _applicationHost;
        private readonly INotificationManager _notificationManager;
        private readonly ILocalizationManager _localizationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingRestartNotifier2"/> class.
        /// </summary>
        /// <param name="applicationHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="notificationManager">Instance of the <see cref="INotificationManager"/> interface.</param>
        /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        public PendingRestartNotifier2(
            IServerApplicationHost applicationHost,
            INotificationManager notificationManager,
            ILocalizationManager localizationManager)
        {
            _applicationHost = applicationHost;
            _notificationManager = notificationManager;
            _localizationManager = localizationManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(PendingRestartEventArgs eventArgs)
        {
            var notification = new NotificationRequest
            {
                NotificationType = NotificationType.ServerRestartRequired.ToString(),
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("ServerNameNeedsToBeRestarted"),
                    _applicationHost.Name)
            };

            await _notificationManager.SendNotification(notification, null, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
