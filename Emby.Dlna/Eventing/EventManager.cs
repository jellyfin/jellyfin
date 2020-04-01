#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

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

        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string requestedTimeoutString, string callbackUrl)
        {
            var subscription = GetSubscription(subscriptionId, false);

            subscription.TimeoutSeconds = ParseTimeout(requestedTimeoutString) ?? 300;
            int timeoutSeconds = subscription.TimeoutSeconds;
            subscription.SubscriptionTime = DateTime.UtcNow;

            _logger.LogDebug(
                "Renewing event subscription for {0} with timeout of {1} to {2}",
                subscription.NotificationType,
                timeoutSeconds,
                subscription.CallbackUrl);

            return GetEventSubscriptionResponse(subscriptionId, requestedTimeoutString, timeoutSeconds);
        }

        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string requestedTimeoutString, string callbackUrl)
        {
            var timeout = ParseTimeout(requestedTimeoutString) ?? 300;
            var id = "uuid:" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            _logger.LogDebug("Creating event subscription for {0} with timeout of {1} to {2}",
                notificationType,
                timeout,
                callbackUrl);

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

                if (int.TryParse(header, NumberStyles.Integer, _usCulture, out var val))
                {
                    return val;
                }
            }

            return null;
        }

        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            _logger.LogDebug("Cancelling event subscription {0}", subscriptionId);

            _subscriptions.TryRemove(subscriptionId, out EventSubscription sub);

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
            response.Headers["TIMEOUT"] = string.IsNullOrEmpty(requestedTimeoutString) ? ("SECOND-" + timeoutSeconds.ToString(_usCulture)) : requestedTimeoutString;

            return response;
        }

        public EventSubscription GetSubscription(string id)
        {
            return GetSubscription(id, false);
        }

        private EventSubscription GetSubscription(string id, bool throwOnMissing)
        {
            if (!_subscriptions.TryGetValue(id, out EventSubscription e) && throwOnMissing)
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
                using (await _httpClient.SendAsync(options, new HttpMethod("NOTIFY")).ConfigureAwait(false))
                {

                }
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
