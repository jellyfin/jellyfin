#pragma warning disable CS1591

using System.Net.Http;
using Emby.Dlna.Eventing;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Service
{
    public class BaseService : IDlnaEventManager
    {
        protected BaseService(ILogger<BaseService> logger, IHttpClientFactory httpClientFactory)
        {
            Logger = logger;
            EventManager = new DlnaEventManager(logger, httpClientFactory);
        }

        protected IDlnaEventManager EventManager { get; }

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
