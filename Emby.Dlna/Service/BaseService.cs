using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dlna;
using Emby.Dlna.Eventing;
using MediaBrowser.Model.Logging;

namespace Emby.Dlna.Service
{
    public class BaseService : IEventManager
    {
        protected IEventManager EventManager;
        protected IHttpClient HttpClient;
        protected ILogger Logger;

        protected BaseService(ILogger logger, IHttpClient httpClient)
        {
            Logger = logger;
            HttpClient = httpClient;  

            EventManager = new EventManager(Logger, HttpClient);
        }

        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            return EventManager.CancelEventSubscription(subscriptionId);
        }

        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string timeoutString)
        {
            return EventManager.RenewEventSubscription(subscriptionId, timeoutString);
        }

        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string timeoutString, string callbackUrl)
        {
            return EventManager.CreateEventSubscription(notificationType, timeoutString, callbackUrl);
        }
    }
}
