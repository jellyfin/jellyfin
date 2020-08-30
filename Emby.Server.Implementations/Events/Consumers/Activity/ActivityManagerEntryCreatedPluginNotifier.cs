using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Notifications;
using Emby.Server.Implementations.Events.ConsumerArgs;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Emby.Server.Implementations.Events.Consumers.Activity
{
    /// <summary>
    /// Notifies plugins that an activity entry was created.
    /// </summary>
    public class ActivityManagerEntryCreatedPluginNotifier : IEventConsumer<ActivityManagerEntryCreatedEventArgs>
    {
        private readonly IConfigurationManager _configurationManager;
        private readonly INotificationManager _notificationManager;
        private readonly string[] _coreNotificationTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityManagerEntryCreatedPluginNotifier"/> class.
        /// </summary>
        /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="notificationManager">Instance of the <see cref="INotificationManager"/> interface.</param>
        public ActivityManagerEntryCreatedPluginNotifier(
            ILocalizationManager localizationManager,
            IConfigurationManager configurationManager,
            INotificationManager notificationManager)
        {
            _configurationManager = configurationManager;
            _notificationManager = notificationManager;
            _coreNotificationTypes = new CoreNotificationTypes(localizationManager)
                .GetNotificationTypes()
                .Select(i => i.Type)
                .ToArray();
        }

        /// <inheritdoc />
        public async Task OnEvent(ActivityManagerEntryCreatedEventArgs eventArgs)
        {
            var type = eventArgs.Argument.Type;

            if (string.IsNullOrEmpty(type) || !_coreNotificationTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var options = _configurationManager.GetConfiguration<NotificationOptions>("notifications");
            if (!eventArgs.Argument.UserId.Equals(Guid.Empty) && !options.IsEnabledToMonitorUser(type, eventArgs.Argument.UserId))
            {
                return;
            }

            var notification = new NotificationRequest
            {
                NotificationType = type,
                Name = eventArgs.Argument.Name,
                Description = eventArgs.Argument.Overview
            };

            await _notificationManager.SendNotification(notification, null, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
