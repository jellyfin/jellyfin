#pragma warning disable CS1591

using Emby.Dlna.Eventing;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Service
{
    public class BaseService : IEventManager
    {
        protected IEventManager _eventManager;
        protected IHttpClient _httpClient;
        protected ILogger _logger;

        protected BaseService(ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _eventManager = new EventManager(logger, _httpClient);
        }

        protected IEventManager EventManager { get; }

        protected IHttpClient HttpClient { get; }

        protected ILogger Logger { get; }

        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            return EventManager.CancelEventSubscription(subscriptionId);
        }

        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string timeoutString, string callbackUrl)
        {
            return EventManager.RenewEventSubscription(subscriptionId, notificationType, timeoutString, callbackUrl);
        }

        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string timeoutString, string callbackUrl)
        {
            return EventManager.CreateEventSubscription(notificationType, timeoutString, callbackUrl);
        }
    }
}
