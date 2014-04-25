using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Notifications
{
    public class NotificationManager : INotificationManager
    {
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private INotificationService[] _services;

        public NotificationManager(ILogManager logManager, IUserManager userManager)
        {
            _userManager = userManager;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public Task SendNotification(NotificationRequest request, CancellationToken cancellationToken)
        {
            var users = request.UserIds.Select(i => _userManager.GetUserById(new Guid(i)));

            var tasks = _services.Select(i => SendNotification(request, i, users, cancellationToken));

            return Task.WhenAll(tasks);
        }

        public Task SendNotification(NotificationRequest request,
            INotificationService service,
            IEnumerable<User> users,
            CancellationToken cancellationToken)
        {
            users = users.Where(i => IsEnabledForUser(service, i))
                .ToList();

            var tasks = users.Select(i => SendNotification(request, service, i, cancellationToken));

            return Task.WhenAll(tasks);

        }

        public async Task SendNotification(NotificationRequest request,
            INotificationService service,
            User user,
            CancellationToken cancellationToken)
        {
            var notification = new UserNotification
            {
                Date = request.Date,
                Description = request.Description,
                Level = request.Level,
                Name = request.Name,
                Url = request.Url,
                User = user
            };

            _logger.Debug("Sending notification via {0} to user {1}", service.Name, user.Name);
            
            try
            {
                await service.SendNotification(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending notification to {0}", ex, service.Name);
            }
        }

        private bool IsEnabledForUser(INotificationService service, User user)
        {
            try
            {
                return service.IsEnabledForUser(user);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in IsEnabledForUser", ex);
                return false;
            }
        }

        public void AddParts(IEnumerable<INotificationService> services)
        {
            _services = services.ToArray();
        }
    }
}
