using MediaBrowser.Model.Dlna;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Dlna
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
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="timeoutSeconds">The timeout seconds.</param>
        /// <returns>EventSubscriptionResponse.</returns>
        EventSubscriptionResponse RenewEventSubscription(string subscriptionId, int? timeoutSeconds);

        /// <summary>
        /// Creates the event subscription.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <param name="timeoutSeconds">The timeout seconds.</param>
        /// <param name="callbackUrl">The callback URL.</param>
        /// <returns>EventSubscriptionResponse.</returns>
        EventSubscriptionResponse CreateEventSubscription(string notificationType, int? timeoutSeconds, string callbackUrl);

        /// <summary>
        /// Gets the subscription.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>EventSubscription.</returns>
        EventSubscription GetSubscription(string id);

        /// <summary>
        /// Triggers the event.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <param name="stateVariables">The state variables.</param>
        /// <returns>Task.</returns>
        Task TriggerEvent(string notificationType, IDictionary<string,string> stateVariables);
    }
}
