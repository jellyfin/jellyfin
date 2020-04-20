#pragma warning disable CS1591

namespace Emby.Dlna
{
    public interface IEventManager
    {
        /// <summary>
        /// Cancels the event subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        EventSubscriptionResponse CancelEventSubscription(string subscriptionId);

        /// <summary>
        /// Renews the event subscription.
        /// </summary>
        EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string requestedTimeoutString, string callbackUrl);

        /// <summary>
        /// Creates the event subscription.
        /// </summary>
        EventSubscriptionResponse CreateEventSubscription(string notificationType, string requestedTimeoutString, string callbackUrl);
    }
}
