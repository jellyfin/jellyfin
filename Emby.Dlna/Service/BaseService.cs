#pragma warning disable CS1591

using Emby.Dlna.Eventing;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Service
{
    public class BaseService : IEventManager
    {
        protected IEventManager EventManager;
        protected IHttpClient HttpClient;
        protected ILogger Logger;

        protected BaseService(ILogger<BaseService> logger, IHttpClient httpClient)
        {
            Logger = logger;
            HttpClient = httpClient;

            EventManager = new EventManager(Logger, HttpClient);
        }

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
