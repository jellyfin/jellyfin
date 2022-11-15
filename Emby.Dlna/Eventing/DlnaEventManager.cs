#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Eventing
{
    public class DlnaEventManager : IDlnaEventManager
    {
        private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions =
            new ConcurrentDictionary<string, EventSubscription>(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public DlnaEventManager(ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string requestedTimeoutString, string callbackUrl)
        {
            var subscription = GetSubscription(subscriptionId, false);
            if (subscription != null)
            {
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

            return new EventSubscriptionResponse(string.Empty, "text/plain");
        }

        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string requestedTimeoutString, string callbackUrl)
        {
            var timeout = ParseTimeout(requestedTimeoutString) ?? 300;
            var id = "uuid:" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            _logger.LogDebug(
                "Creating event subscription for {0} with timeout of {1} to {2}",
                notificationType,
                timeout,
                callbackUrl);

            _subscriptions.TryAdd(id, new EventSubscription
            {
                Id = id,
                CallbackUrl = callbackUrl,
                SubscriptionTime = DateTime.UtcNow,
                TimeoutSeconds = timeout,
                NotificationType = notificationType
            });

            return GetEventSubscriptionResponse(id, requestedTimeoutString, timeout);
        }

        private int? ParseTimeout(string header)
        {
            if (!string.IsNullOrEmpty(header))
            {
                // Starts with SECOND-
                if (int.TryParse(header.AsSpan().RightPart('-'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                {
                    return val;
                }
            }

            return null;
        }

        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            _logger.LogDebug("Cancelling event subscription {0}", subscriptionId);

            _subscriptions.TryRemove(subscriptionId, out _);

            return new EventSubscriptionResponse(string.Empty, "text/plain");
        }

        private EventSubscriptionResponse GetEventSubscriptionResponse(string subscriptionId, string requestedTimeoutString, int timeoutSeconds)
        {
            var response = new EventSubscriptionResponse(string.Empty, "text/plain");

            response.Headers["SID"] = subscriptionId;
            response.Headers["TIMEOUT"] = string.IsNullOrEmpty(requestedTimeoutString) ? ("SECOND-" + timeoutSeconds.ToString(CultureInfo.InvariantCulture)) : requestedTimeoutString;

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
                .Where(i => !i.IsExpired && string.Equals(notificationType, i.NotificationType, StringComparison.OrdinalIgnoreCase));

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
                builder.Append("<e:property>")
                    .Append('<')
                    .Append(key)
                    .Append('>')
                    .Append(stateVariables[key])
                    .Append("</")
                    .Append(key)
                    .Append('>')
                    .Append("</e:property>");
            }

            builder.Append("</e:propertyset>");

            using var options = new HttpRequestMessage(new HttpMethod("NOTIFY"), subscription.CallbackUrl);
            options.Content = new StringContent(builder.ToString(), Encoding.UTF8, MediaTypeNames.Text.Xml);
            options.Headers.TryAddWithoutValidation("NT", subscription.NotificationType);
            options.Headers.TryAddWithoutValidation("NTS", "upnp:propchange");
            options.Headers.TryAddWithoutValidation("SID", subscription.Id);
            options.Headers.TryAddWithoutValidation("SEQ", subscription.TriggerCount.ToString(CultureInfo.InvariantCulture));

            try
            {
                using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                    .SendAsync(options, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
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
