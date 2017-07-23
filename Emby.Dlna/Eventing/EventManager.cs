using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.Dlna.Eventing
{
    public class EventManager : IEventManager
    {
        private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions =
            new ConcurrentDictionary<string, EventSubscription>(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public EventManager(ILogger logger, IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string requestedTimeoutString)
        {
            var subscription = GetSubscription(subscriptionId, true);

            // Remove logging for now because some devices are sending this very frequently
            // TODO re-enable with dlna debug logging setting
            //_logger.Debug("Renewing event subscription for {0} with timeout of {1} to {2}",
            //    subscription.NotificationType,
            //    timeout,
            //    subscription.CallbackUrl);

            subscription.TimeoutSeconds = ParseTimeout(requestedTimeoutString) ?? 300;
            subscription.SubscriptionTime = DateTime.UtcNow;

            return GetEventSubscriptionResponse(subscriptionId, requestedTimeoutString, subscription.TimeoutSeconds);
        }

        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string requestedTimeoutString, string callbackUrl)
        {
            var timeout = ParseTimeout(requestedTimeoutString) ?? 300;
            var id = "uuid:" + Guid.NewGuid().ToString("N");

            // Remove logging for now because some devices are sending this very frequently
            // TODO re-enable with dlna debug logging setting
            //_logger.Debug("Creating event subscription for {0} with timeout of {1} to {2}",
            //    notificationType,
            //    timeout,
            //    callbackUrl);

            _subscriptions.TryAdd(id, new EventSubscription
            {
                Id = id,
                CallbackUrl = callbackUrl,
                SubscriptionTime = DateTime.UtcNow,
                TimeoutSeconds = timeout
            });

            return GetEventSubscriptionResponse(id, requestedTimeoutString, timeout);
        }

        private int? ParseTimeout(string header)
        {
            if (!string.IsNullOrEmpty(header))
            {
                // Starts with SECOND-
                header = header.Split('-').Last();

                int val;

                if (int.TryParse(header, NumberStyles.Any, _usCulture, out val))
                {
                    return val;
                }
            }

            return null;
        }

        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            _logger.Debug("Cancelling event subscription {0}", subscriptionId);

            EventSubscription sub;
            _subscriptions.TryRemove(subscriptionId, out sub);

            return new EventSubscriptionResponse
            {
                Content = string.Empty,
                ContentType = "text/plain"
            };
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private EventSubscriptionResponse GetEventSubscriptionResponse(string subscriptionId, string requestedTimeoutString, int timeoutSeconds)
        {
            var response = new EventSubscriptionResponse
            {
                Content = string.Empty,
                ContentType = "text/plain"
            };

            response.Headers["SID"] = subscriptionId;
            response.Headers["TIMEOUT"] = string.IsNullOrWhiteSpace(requestedTimeoutString) ? ("SECOND-" + timeoutSeconds.ToString(_usCulture)) : requestedTimeoutString;

            return response;
        }

        public EventSubscription GetSubscription(string id)
        {
            return GetSubscription(id, false);
        }

        private EventSubscription GetSubscription(string id, bool throwOnMissing)
        {
            EventSubscription e;

            if (!_subscriptions.TryGetValue(id, out e) && throwOnMissing)
            {
                throw new ResourceNotFoundException("Event with Id " + id + " not found.");
            }

            return e;
        }

        public Task TriggerEvent(string notificationType, IDictionary<string, string> stateVariables)
        {
            var subs = _subscriptions.Values
                .Where(i => !i.IsExpired && string.Equals(notificationType, i.NotificationType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var tasks = subs.Select(i => TriggerEvent(i, stateVariables));

            return Task.WhenAll(tasks);
        }

        private async Task TriggerEvent(EventSubscription subscription, IDictionary<string, string> stateVariables)
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\"?>");
            builder.Append("<e:propertyset xmlns:e=\"urn:schemas-upnp-org:event-1-0\">");
            foreach (var key in stateVariables.Keys)
            {
                builder.Append("<e:property>");
                builder.Append("<" + key + ">");
                builder.Append(stateVariables[key]);
                builder.Append("</" + key + ">");
                builder.Append("</e:property>");
            }
            builder.Append("</e:propertyset>");

            var options = new HttpRequestOptions
            {
                RequestContent = builder.ToString(),
                RequestContentType = "text/xml",
                Url = subscription.CallbackUrl,
                BufferContent = false
            };

            options.RequestHeaders.Add("NT", subscription.NotificationType);
            options.RequestHeaders.Add("NTS", "upnp:propchange");
            options.RequestHeaders.Add("SID", subscription.Id);
            options.RequestHeaders.Add("SEQ", subscription.TriggerCount.ToString(_usCulture));

            try
            {
                await _httpClient.SendAsync(options, "NOTIFY").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Already logged at lower levels
            }
            finally
            {
                subscription.IncrementTriggerCount();
            }
        }
    }
}
