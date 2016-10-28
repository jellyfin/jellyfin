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

namespace MediaBrowser.Api
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
    public class GetNotificationServices : IReturn<List<NotificationServiceInfo>>
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
    public class NotificationsService : BaseApiService
    {
        private readonly INotificationsRepository _notificationsRepo;
        private readonly INotificationManager _notificationManager;
        private readonly IUserManager _userManager;

        public NotificationsService(INotificationsRepository notificationsRepo, INotificationManager notificationManager, IUserManager userManager)
        {
            _notificationsRepo = notificationsRepo;
            _notificationManager = notificationManager;
            _userManager = userManager;
        }

        public object Get(GetNotificationTypes request)
        {
            var result = _notificationManager.GetNotificationTypes().ToList();

            return ToOptimizedResult(result);
        }

        public object Get(GetNotificationServices request)
        {
            var result = _notificationManager.GetNotificationServices().ToList();

            return ToOptimizedResult(result);
        }

        public object Get(GetNotificationsSummary request)
        {
            var result = _notificationsRepo.GetNotificationsSummary(request.UserId);

            return ToOptimizedResult(result);
        }

        public void Post(AddAdminNotification request)
        {
            // This endpoint really just exists as post of a real with sickbeard
            var task = AddNotification(request);

            Task.WaitAll(task);
        }

        private async Task AddNotification(AddAdminNotification request)
        {
            var notification = new NotificationRequest
            {
                Date = DateTime.UtcNow,
                Description = request.Description,
                Level = request.Level,
                Name = request.Name,
                Url = request.Url,
                UserIds = _userManager.Users.Where(i => i.Policy.IsAdministrator).Select(i => i.Id.ToString("N")).ToList()
            };

            await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(MarkRead request)
        {
            var task = MarkRead(request.Ids, request.UserId, true);

            Task.WaitAll(task);
        }

        public void Post(MarkUnread request)
        {
            var task = MarkRead(request.Ids, request.UserId, false);

            Task.WaitAll(task);
        }

        private Task MarkRead(string idList, string userId, bool read)
        {
            var ids = (idList ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (ids.Length == 0)
            {
                return _notificationsRepo.MarkAllRead(userId, read, CancellationToken.None);
            }

            return _notificationsRepo.MarkRead(ids, userId, read, CancellationToken.None);
        }

        public object Get(GetNotifications request)
        {
            var result = _notificationsRepo.GetNotifications(new NotificationQuery
            {
                IsRead = request.IsRead,
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                UserId = request.UserId
            });

            return ToOptimizedResult(result);
        }
    }
}
