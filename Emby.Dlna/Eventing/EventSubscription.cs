using System;

namespace Emby.Dlna.Eventing
{
    public class EventSubscription
    {
        public string Id { get; set; }
        public string CallbackUrl { get; set; }
        public string NotificationType { get; set; }

        public DateTime SubscriptionTime { get; set; }
        public int TimeoutSeconds { get; set; }

        public long TriggerCount { get; set; }

        public void IncrementTriggerCount()
        {
            if (TriggerCount == long.MaxValue)
            {
                TriggerCount = 0;
            }

            TriggerCount++;
        }

        public bool IsExpired
        {
            get
            {
                return SubscriptionTime.AddSeconds(TimeoutSeconds) >= DateTime.UtcNow;
            }
        }
    }
}
