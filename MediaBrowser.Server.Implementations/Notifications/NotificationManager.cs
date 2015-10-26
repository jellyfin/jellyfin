using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
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
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Server.Implementations.Notifications
{
    public class NotificationManager : INotificationManager
    {
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly IServerConfigurationManager _config;

        private INotificationService[] _services;
        private INotificationTypeFactory[] _typeFactories;

        public NotificationManager(ILogManager logManager, IUserManager userManager, IServerConfigurationManager config)
        {
            _userManager = userManager;
            _config = config;
            _logger = logManager.GetLogger(GetType().Name);
        }

        private NotificationOptions GetConfiguration()
        {
            return _config.GetConfiguration<NotificationOptions>("notifications");
        }

        public Task SendNotification(NotificationRequest request, CancellationToken cancellationToken)
        {
            var notificationType = request.NotificationType;

            var options = string.IsNullOrWhiteSpace(notificationType) ?
                null :
                GetConfiguration().GetOptions(notificationType);

            var users = GetUserIds(request, options)
                .Select(i => _userManager.GetUserById(i));

            var title = GetTitle(request, options);
            var description = GetDescription(request, options);

            var tasks = _services.Where(i => IsEnabled(i, notificationType))
                .Select(i => SendNotification(request, i, users, title, description, cancellationToken));

            return Task.WhenAll(tasks);
        }

        private Task SendNotification(NotificationRequest request,
            INotificationService service,
            IEnumerable<User> users,
            string title,
            string description,
            CancellationToken cancellationToken)
        {
            users = users.Where(i => IsEnabledForUser(service, i))
                .ToList();

            var tasks = users.Select(i => SendNotification(request, service, title, description, i, cancellationToken));

            return Task.WhenAll(tasks);

        }

        private IEnumerable<string> GetUserIds(NotificationRequest request, NotificationOption options)
        {
            if (request.SendToUserMode.HasValue)
            {
                switch (request.SendToUserMode.Value)
                {
                    case SendToUserType.Admins:
                        return _userManager.Users.Where(i => i.Policy.IsAdministrator)
                                .Select(i => i.Id.ToString("N"));
                    case SendToUserType.All:
                        return _userManager.Users.Select(i => i.Id.ToString("N"));
                    case SendToUserType.Custom:
                        return request.UserIds;
                    default:
                        throw new ArgumentException("Unrecognized SendToUserMode: " + request.SendToUserMode.Value);
                }
            }

            if (options != null && !string.IsNullOrWhiteSpace(request.NotificationType))
            {
                var config = GetConfiguration();

                return _userManager.Users
                    .Where(i => config.IsEnabledToSendToUser(request.NotificationType, i.Id.ToString("N"), i.Policy))
                    .Select(i => i.Id.ToString("N"));
            }

            return request.UserIds;
        }

        private async Task SendNotification(NotificationRequest request,
            INotificationService service,
            string title,
            string description,
            User user,
            CancellationToken cancellationToken)
        {
            var notification = new UserNotification
            {
                Date = request.Date,
                Description = description,
                Level = request.Level,
                Name = title,
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

        private string GetTitle(NotificationRequest request, NotificationOption options)
        {
            var title = request.Name;

            // If empty, grab from options 
            if (string.IsNullOrEmpty(title))
            {
                if (!string.IsNullOrEmpty(request.NotificationType))
                {
                    if (options != null)
                    {
                        title = options.Title;
                    }
                }
            }

            // If still empty, grab default
            if (string.IsNullOrEmpty(title))
            {
                if (!string.IsNullOrEmpty(request.NotificationType))
                {
                    var info = GetNotificationTypes().FirstOrDefault(i => string.Equals(i.Type, request.NotificationType, StringComparison.OrdinalIgnoreCase));

                    if (info != null)
                    {
                        title = info.DefaultTitle;
                    }
                }
            }

            title = title ?? string.Empty;

            foreach (var pair in request.Variables)
            {
                var token = "{" + pair.Key + "}";

                title = title.Replace(token, pair.Value, StringComparison.OrdinalIgnoreCase);
            }

            return title;
        }

        private string GetDescription(NotificationRequest request, NotificationOption options)
        {
            var text = request.Description;

            // If empty, grab from options 
            if (string.IsNullOrEmpty(text))
            {
                if (!string.IsNullOrEmpty(request.NotificationType))
                {
                    if (options != null)
                    {
                        text = options.Description;
                    }
                }
            }

            // If still empty, grab default
            if (string.IsNullOrEmpty(text))
            {
                if (!string.IsNullOrEmpty(request.NotificationType))
                {
                    var info = GetNotificationTypes().FirstOrDefault(i => string.Equals(i.Type, request.NotificationType, StringComparison.OrdinalIgnoreCase));

                    if (info != null)
                    {
                        text = info.DefaultDescription;
                    }
                }
            }

            text = text ?? string.Empty;

            foreach (var pair in request.Variables)
            {
                var token = "{" + pair.Key + "}";

                text = text.Replace(token, pair.Value, StringComparison.OrdinalIgnoreCase);
            }

            return text;
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

        private bool IsEnabled(INotificationService service, string notificationType)
        {
            if (string.IsNullOrEmpty(notificationType))
            {
                return true;
            }

            var configurable = service as IConfigurableNotificationService;

            if (configurable != null)
            {
                return configurable.IsEnabled(notificationType);
            }

            return GetConfiguration().IsServiceEnabled(service.Name, notificationType);
        }

        public void AddParts(IEnumerable<INotificationService> services, IEnumerable<INotificationTypeFactory> notificationTypeFactories)
        {
            _services = services.ToArray();
            _typeFactories = notificationTypeFactories.ToArray();
        }

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            var list = _typeFactories.Select(i =>
            {
                try
                {
                    return i.GetNotificationTypes().ToList();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in GetNotificationTypes", ex);
                    return new List<NotificationTypeInfo>();
                }

            }).SelectMany(i => i).ToList();

            var config = GetConfiguration();

            foreach (var i in list)
            {
                i.Enabled = config.IsEnabled(i.Type);
            }

            return list;
        }

        public IEnumerable<NotificationServiceInfo> GetNotificationServices()
        {
            return _services.Where(i =>
            {
                var configurable = i as IConfigurableNotificationService;

                return configurable == null || !configurable.IsHidden;

            }).Select(i => new NotificationServiceInfo
            {
                Name = i.Name,
                Id = i.Name.GetMD5().ToString("N")

            }).OrderBy(i => i.Name);
        }
    }
}
