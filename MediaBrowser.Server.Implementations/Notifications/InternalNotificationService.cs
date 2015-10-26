using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace MediaBrowser.Server.Implementations.Notifications
{
    public class InternalNotificationService : INotificationService, IConfigurableNotificationService
    {
        private readonly INotificationsRepository _repo;

        public InternalNotificationService(INotificationsRepository repo)
        {
            _repo = repo;
        }

        public string Name
        {
            get { return "Dashboard Notifications"; }
        }

        public Task SendNotification(UserNotification request, CancellationToken cancellationToken)
        {
            return _repo.AddNotification(new Notification
            {
                Date = request.Date,
                Description = request.Description,
                Level = request.Level,
                Name = request.Name,
                Url = request.Url,
                UserId = request.User.Id.ToString("N")

            }, cancellationToken);
        }

        public bool IsEnabledForUser(User user)
        {
            return user.Policy.IsAdministrator;
        }

        public bool IsHidden
        {
            get { return true; }
        }

        public bool IsEnabled(string notificationType)
        {
            if (notificationType.IndexOf("playback", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }
            if (notificationType.IndexOf("newlibrarycontent", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }
            return true;
        }
    }
}
