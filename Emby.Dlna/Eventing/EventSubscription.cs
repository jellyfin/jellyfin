using System;

namespace Emby.Dlna.Eventing
{
    /// <summary>
    /// Defines the <see cref="EventSubscription" />.
    /// </summary>
    public class EventSubscription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscription"/> class.
        /// </summary>
        /// <param name="id">The id of the event.</param>
        /// <param name="callbackurl">The callback url for the event.</param>
        /// <param name="subscriptionTime">The subscription time of the event.</param>
        /// <param name="timeout">The timeout value for the event.</param>
        public EventSubscription(string id, string callbackurl, DateTime subscriptionTime, int timeout)
        {
            Id = id;
            CallbackUrl = callbackurl;
            SubscriptionTime = subscriptionTime;
            TimeoutSeconds = timeout;
        }

        /// <summary>
        /// Gets or sets the event subscription Id..
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the url the event is to use to notify the client..
        /// </summary>
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the type of notification..
        /// </summary>
        public string? NotificationType { get; set; }

        /// <summary>
        /// Gets or sets the length of the subscription..
        /// </summary>
        public DateTime SubscriptionTime { get; set; }

        /// <summary>
        /// Gets or sets the timeout of the subscription..
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the number of times this event has triggered..
        /// </summary>
        public long TriggerCount { get; set; }

        /// <summary>
        /// Gets a value indicating whether this event is expirted..
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
