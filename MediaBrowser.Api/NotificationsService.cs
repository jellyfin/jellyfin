using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack;

namespace MediaBrowser.Api
{
    [Route("/Notifications/{UserId}", "GET")]
    [Api(Description = "Gets notifications")]
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

    [Route("/Notifications/{UserId}/Summary", "GET")]
    [Api(Description = "Gets a notification summary for a user")]
    public class GetNotificationsSummary : IReturn<NotificationsSummary>
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Route("/Notifications/{UserId}", "POST")]
    [Api(Description = "Adds a notifications")]
    public class AddUserNotification : IReturn<Notification>
    {
        [ApiMember(Name = "Id", Description = "The Id of the new notification. If unspecified one will be provided.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public Guid? Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "Name", Description = "The notification's name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Name { get; set; }

        [ApiMember(Name = "Description", Description = "The notification's description", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Description { get; set; }

        [ApiMember(Name = "Url", Description = "The notification's info url", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Url { get; set; }

        [ApiMember(Name = "Category", Description = "The notification's category", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Category { get; set; }

        [ApiMember(Name = "RelatedId", Description = "The notification's related id (item)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string RelatedId { get; set; }

        [ApiMember(Name = "Level", Description = "The notification level", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public NotificationLevel Level { get; set; }
    }
    
    [Route("/Notifications/{UserId}/Read", "POST")]
    [Api(Description = "Marks notifications as read")]
    public class MarkRead : IReturnVoid
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "Ids", Description = "A list of notification ids, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
    }

    [Route("/Notifications/{UserId}/Unread", "POST")]
    [Api(Description = "Marks notifications as unread")]
    public class MarkUnread : IReturnVoid
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "Ids", Description = "A list of notification ids, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
    }

    public class NotificationsService : BaseApiService
    {
        private readonly INotificationsRepository _notificationsRepo;

        public NotificationsService(INotificationsRepository notificationsRepo)
        {
            _notificationsRepo = notificationsRepo;
        }

        public object Post(AddUserNotification request)
        {
            var task = AddNotification(request);

            return ToOptimizedResult(task.Result);
        }

        public object Get(GetNotificationsSummary request)
        {
            var result = _notificationsRepo.GetNotificationsSummary(request.UserId);

            return result;
        }

        private async Task<Notification> AddNotification(AddUserNotification request)
        {
            var notification = new Notification
            {
                Id = request.Id ?? Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Description = request.Description,
                Level = request.Level,
                Name = request.Name,
                Url = request.Url,
                UserId = request.UserId,
                Category = request.Category,
                RelatedId = request.RelatedId
            };

            await _notificationsRepo.AddNotification(notification, CancellationToken.None).ConfigureAwait(false);

            return notification;
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

        private Task MarkRead(string idList, Guid userId, bool read)
        {
            var ids = idList.Split(',').Select(i => new Guid(i));

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
