#pragma warning disable CS1591

using System;

namespace Emby.Dlna.Eventing
{
    public class EventSubscription
    {
        public EventSubscription(string id, string callbackUrl, DateTime subscriptionTime, int timeoutSeconds)
        {
            Id = id;
            CallbackUrl = callbackUrl;
            SubscriptionTime = subscriptionTime;
            TimeoutSeconds = timeoutSeconds;
        }

        public string Id { get; set; }

        public string CallbackUrl { get; set; }

        // TODO what should NotificationType be? It is never set in code.
        public string? NotificationType { get; set; }

        public DateTime SubscriptionTime { get; set; }

        public int TimeoutSeconds { get; set; }

        public long TriggerCount { get; set; }

        public bool IsExpired => SubscriptionTime.AddSeconds(TimeoutSeconds) >= DateTime.UtcNow;

        public void IncrementTriggerCount()
        {
            if (TriggerCount == long.MaxValue)
            {
                TriggerCount = 0;
            }

            TriggerCount++;
        }
    }
}
