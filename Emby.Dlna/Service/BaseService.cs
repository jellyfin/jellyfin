#pragma warning disable CS1591

using Emby.Dlna.Eventing;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Service
{
    public class BaseService : IDlnaEventManager
    {
        protected IDlnaEventManager _dlnaEventManager;
        protected IHttpClient HttpClient;
        protected ILogger Logger;

        protected BaseService(ILogger<BaseService> logger, IHttpClient httpClient)
        {
            Logger = logger;
            HttpClient = httpClient;

            _dlnaEventManager = new DlnaEventManager(logger, HttpClient);
        }

        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            return _dlnaEventManager.CancelEventSubscription(subscriptionId);
        }

        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string timeoutString, string callbackUrl)
        {
            return _dlnaEventManager.RenewEventSubscription(subscriptionId, notificationType, timeoutString, callbackUrl);
        }

        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string timeoutString, string callbackUrl)
        {
            return _dlnaEventManager.CreateEventSubscription(notificationType, timeoutString, callbackUrl);
        }
    }
}
