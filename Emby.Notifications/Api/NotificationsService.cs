using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Dto;

namespace Emby.Notifications.Api
{
    [Route("/Notifications/{UserId}", "GET", Summary = "Gets notifications")]
    public class GetNotifications : IReturn<NotificationResult>
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "IsRead", Description = "An optional filter by IsRead", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsRead { get; set; }

        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
    }

    public class Notification
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public DateTime Date { get; set; }

        public bool IsRead { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public NotificationLevel Level { get; set; }
    }

    public class NotificationResult
    {
        public Notification[] Notifications { get; set; }
        public int TotalRecordCount { get; set; }
    }

    public class NotificationsSummary
    {
        public int UnreadCount { get; set; }
        public NotificationLevel MaxUnreadNotificationLevel { get; set; }
    }

    [Route("/Notifications/{UserId}/Summary", "GET", Summary = "Gets a notification summary for a user")]
    public class GetNotificationsSummary : IReturn<NotificationsSummary>
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }
    }

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
        public string Name { get; set; }

        [ApiMember(Name = "Description", Description = "The notification's description", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Description { get; set; }

        [ApiMember(Name = "ImageUrl", Description = "The notification's image url", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ImageUrl { get; set; }

        [ApiMember(Name = "Url", Description = "The notification's info url", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Url { get; set; }

        [ApiMember(Name = "Level", Description = "The notification level", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public NotificationLevel Level { get; set; }
    }

    [Route("/Notifications/{UserId}/Read", "POST", Summary = "Marks notifications as read")]
    public class MarkRead : IReturnVoid
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        [ApiMember(Name = "Ids", Description = "A list of notification ids, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
    }

    [Route("/Notifications/{UserId}/Unread", "POST", Summary = "Marks notifications as unread")]
    public class MarkUnread : IReturnVoid
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        [ApiMember(Name = "Ids", Description = "A list of notification ids, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
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
            return _notificationManager.GetNotificationTypes();
        }

        public object Get(GetNotificationServices request)
        {
            return _notificationManager.GetNotificationServices().ToList();
        }

        public object Get(GetNotificationsSummary request)
        {
            return new NotificationsSummary
            {

            };
        }

        public Task Post(AddAdminNotification request)
        {
            // This endpoint really just exists as post of a real with sickbeard
            return AddNotification(request);
        }

        private Task AddNotification(AddAdminNotification request)
        {
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

        public void Post(MarkRead request)
        {
        }

        public void Post(MarkUnread request)
        {
        }

        public object Get(GetNotifications request)
        {
            return new NotificationResult();
        }
    }
}
