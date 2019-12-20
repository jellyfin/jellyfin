#pragma warning disable CS1591
#pragma warning disable SA1402
#pragma warning disable SA1600
#pragma warning disable SA1649

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Services;

namespace Emby.Notifications.Api
{
    [Route("/Notifications/Types", "GET", Summary = "Gets notification types")]
    public class GetNotificationTypes : IReturn<List<NotificationTypeInfo>>
    {
    }

    [Route("/Notifications/Services", "GET", Summary = "Gets notification types")]
    public class GetNotificationServices : IReturn<List<NameIdPair>>
    {
    }

    [Route("/Notifications/Admin", "POST", Summary = "Sends a notification to all admin users")]
    public class AddAdminNotification : IReturnVoid
    {
        [ApiMember(Name = "Name", Description = "The notification's name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Name { get; set; } = string.Empty;

        [ApiMember(Name = "Description", Description = "The notification's description", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Description { get; set; } = string.Empty;

        [ApiMember(Name = "ImageUrl", Description = "The notification's image url", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string? ImageUrl { get; set; }

        [ApiMember(Name = "Url", Description = "The notification's info url", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string? Url { get; set; }

        [ApiMember(Name = "Level", Description = "The notification level", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public NotificationLevel Level { get; set; }
    }

    [Authenticated]
    public class NotificationsService : IService
    {
        private readonly INotificationManager _notificationManager;
        private readonly IUserManager _userManager;

        public NotificationsService(INotificationManager notificationManager, IUserManager userManager)
        {
            _notificationManager = notificationManager;
            _userManager = userManager;
        }

        public object Get(GetNotificationTypes request)
        {
            _ = request; // Silence unused variable warning
            return _notificationManager.GetNotificationTypes();
        }

        public object Get(GetNotificationServices request)
        {
            _ = request; // Silence unused variable warning
            return _notificationManager.GetNotificationServices().ToList();
        }

        public Task Post(AddAdminNotification request)
        {
            // This endpoint really just exists as post of a real with sickbeard
            var notification = new NotificationRequest
            {
                Date = DateTime.UtcNow,
                Description = request.Description,
                Level = request.Level,
                Name = request.Name,
                Url = request.Url,
                UserIds = _userManager.Users.Where(i => i.Policy.IsAdministrator).Select(i => i.Id).ToArray()
            };

            return _notificationManager.SendNotification(notification, CancellationToken.None);
        }
    }
}
