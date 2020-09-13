#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Eventing
{
    /// <summary>
    /// Defines the <see cref="DlnaEventManager"/> class.
    /// </summary>
    public class DlnaEventManager : IDlnaEventManager
    {
        private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions =
            new ConcurrentDictionary<string, EventSubscription>(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaEventManager"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
        public DlnaEventManager(ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new renewal subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription id previously assigned.</param>
        /// <param name="notificationType">The notification type to renew.</param>
        /// <param name="requestedTimeoutString">The timeout assigned.</param>
        /// <param name="callbackUrl">The url which should be called at notifications.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the renewal information.</returns>
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

            return new EventSubscriptionResponse
            {
                Content = string.Empty,
                ContentType = "text/plain"
            };
        }

        /// <summary>
        /// Creates a new renewal subscription.
        /// </summary>
        /// <param name="notificationType">The notification type to renew.</param>
        /// <param name="requestedTimeoutString">The timeout assigned.</param>
        /// <param name="callbackUrl">The url which should be called at notifications.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the subscription information.</returns>
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
                TimeoutSeconds = timeout
            });

            return GetEventSubscriptionResponse(id, requestedTimeoutString, timeout);
        }

        /// <summary>
        /// Parses a SSDP formatted time string.
        /// </summary>
        /// <param name="header">String to parse.</param>
        /// <returns>The value, or null if no value found.</returns>
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

        /// <summary>
        /// Cancels a subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription id previously assigned.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the cancellation information.</returns>
        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            _logger.LogDebug("Cancelling event subscription {0}", subscriptionId);

            _subscriptions.TryRemove(subscriptionId, out _);

            return new EventSubscriptionResponse
            {
                Content = string.Empty,
                ContentType = "text/plain"
            };
        }

        /// <summary>
        /// Creates a event subscription response.
        /// </summary>
        /// <param name="subscriptionId">The subscription id previously assigned.</param>
        /// <param name="requestedTimeoutString">The timeout assigned.</param>
        /// <param name="timeoutSeconds">An alternative timeout to use, if requestedTimeoutString is empty.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the cancellation information.</returns>
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

        /// <summary>
        /// Returns the event subscription record for the id provided.
        /// </summary>
        /// <param name="id">The id of the subscription.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the record, or null if not found.</returns>
        public EventSubscription? GetSubscription(string id)
        {
            return GetSubscription(id, false);
        }

        /// <summary>
        /// Returns the event subscription record for the id provided.
        /// </summary>
        /// <param name="id">The id of the subscription.</param>
        /// <param name="throwOnMissing">Set to true, if an exception is to be thrown if the id cannot be located.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the record, or null if not found.</returns>
        private EventSubscription? GetSubscription(string id, bool throwOnMissing)
        {
            if (!_subscriptions.TryGetValue(id, out EventSubscription e) && throwOnMissing)
            {
                throw new ResourceNotFoundException("Event with Id " + id + " not found.");
            }

            return e;
        }

        /// <summary>
        /// Triggers an event.
        /// </summary>
        /// <param name="notificationType">The event notification type.</param>
        /// <param name="stateVariables">The state variables to include with the event.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the record, or null if not found.</returns>
        public Task TriggerEvent(string notificationType, IDictionary<string, string> stateVariables)
        {
            var subs = _subscriptions.Values
                .Where(i => !i.IsExpired && string.Equals(notificationType, i.NotificationType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var tasks = subs.Select(i => TriggerEvent(i, stateVariables));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Triggers an event.
        /// </summary>
        /// <param name="subscription">The <see cref="EventSubscription"/> information to use to trigger an event.</param>
        /// <param name="stateVariables">The state variables to include with the event.</param>
        /// <returns>An <see cref="EventSubscriptionResponse"/> containing the record, or null if not found.</returns>
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

            using var options = new HttpRequestMessage(new HttpMethod("NOTIFY"),  subscription.CallbackUrl);
            options.Content = new StringContent(builder.ToString(), Encoding.UTF8, MediaTypeNames.Text.Xml);
            options.Headers.TryAddWithoutValidation("NT", subscription.NotificationType);
            options.Headers.TryAddWithoutValidation("NTS", "upnp:propchange");
            options.Headers.TryAddWithoutValidation("SID", subscription.Id);
            options.Headers.TryAddWithoutValidation("SEQ", subscription.TriggerCount.ToString(_usCulture));

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
