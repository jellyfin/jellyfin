using System;

namespace Emby.Dlna.Eventing
{
    /// <summary>
    /// Defines the <see cref="EventSubscription" />.
    /// </summary>
    public class EventSubscription
    {
        /// <summary>
        /// Gets or sets the event subscription Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the url the event is to use to notify the client.
        /// </summary>
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the type of notification.
        /// </summary>
        public string NotificationType { get; set; }

        /// <summary>
        /// Gets or sets the length of the subscription.
        /// </summary>
        public DateTime SubscriptionTime { get; set; }

        /// <summary>
        /// Gets or sets the timeout of the subscription.
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the number of times this event has triggered.
        /// </summary>
        public long TriggerCount { get; set; }

        /// <summary>
        /// Gets a value indicating whether this event is expirted.
        /// </summary>
        public bool IsExpired => SubscriptionTime.AddSeconds(TimeoutSeconds) >= DateTime.UtcNow;

        /// <summary>
        /// Increments the trigger count.
        /// </summary>
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
