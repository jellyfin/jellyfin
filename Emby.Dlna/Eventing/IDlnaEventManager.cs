using Emby.Dlna.Eventing;
using Emby.Dlna.Service;

namespace Emby.Dlna.Eventing
{
    /// <summary>
    /// Interface for <see cref="DlnaEventManager"/>.
    /// </summary>
    public interface IDlnaEventManager
    {
        /// <summary>
        /// Cancels the event subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <returns>The response.</returns>
        EventSubscriptionResponse CancelEventSubscription(string subscriptionId);

        /// <summary>
        /// Renews the event subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="requestedTimeoutString">The requested timeout as a sting.</param>
        /// <param name="callbackUrl">The callback url.</param>
        /// <returns>The response.</returns>
        EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string requestedTimeoutString, string callbackUrl);

        /// <summary>
        /// Creates the event subscription.
        /// </summary>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="requestedTimeoutString">The requested timeout as a sting.</param>
        /// <param name="callbackUrl">The callback url.</param>
        /// <returns>The response.</returns>
        EventSubscriptionResponse CreateEventSubscription(string notificationType, string requestedTimeoutString, string callbackUrl);
    }
}
