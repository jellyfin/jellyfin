#pragma warning disable CS1591

using System;

namespace Emby.Dlna.Eventing
{
    public class EventSubscription
    {
        public string Id { get; set; } = string.Empty;

        public string CallbackUrl { get; set; } = string.Empty;

        public string NotificationType { get; set; } = string.Empty;

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
