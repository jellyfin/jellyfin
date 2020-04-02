#pragma warning disable CS1591
#pragma warning disable SA1402
#pragma warning disable SA1649

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    [Route("/Notifications/{UserId}", "GET", Summary = "Gets notifications")]
    public class GetNotifications : IReturn<NotificationResult>
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; } = string.Empty;

        [ApiMember(Name = "IsRead", Description = "An optional filter by IsRead", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsRead { get; set; }

        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
    }

    public class Notification
    {
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public bool IsRead { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public NotificationLevel Level { get; set; }
    }

    public class NotificationResult
    {
        public IReadOnlyList<Notification> Notifications { get; set; } = Array.Empty<Notification>();

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
        public string UserId { get; set; } = string.Empty;
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

    [Route("/Notifications/{UserId}/Read", "POST", Summary = "Marks notifications as read")]
    public class MarkRead : IReturnVoid
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; } = string.Empty;

        [ApiMember(Name = "Ids", Description = "A list of notification ids, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; } = string.Empty;
    }

    [Route("/Notifications/{UserId}/Unread", "POST", Summary = "Marks notifications as unread")]
    public class MarkUnread : IReturnVoid
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; } = string.Empty;

        [ApiMember(Name = "Ids", Description = "A list of notification ids, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; } = string.Empty;
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

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetNotificationTypes request)
        {
            return _notificationManager.GetNotificationTypes();
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetNotificationServices request)
        {
            return _notificationManager.GetNotificationServices().ToList();
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetNotificationsSummary request)
        {
            return new NotificationsSummary
            {
            };
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

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public void Post(MarkRead request)
        {
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public void Post(MarkUnread request)
        {
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetNotifications request)
        {
            return new NotificationResult();
        }
    }
}
