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
        protected ILogger Logger;

        protected BaseService(ILogger<BaseService> logger, IHttpClient httpClient)
        {
            Logger = logger;
            _httpClient = httpClient;

            _eventManager = new EventManager(logger, _httpClient);
        }

        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            return _eventManager.CancelEventSubscription(subscriptionId);
        }

        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string timeoutString, string callbackUrl)
        {
            return _eventManager.RenewEventSubscription(subscriptionId, notificationType, timeoutString, callbackUrl);
        }

        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string timeoutString, string callbackUrl)
        {
            return _eventManager.CreateEventSubscription(notificationType, timeoutString, callbackUrl);
        }
    }
}
