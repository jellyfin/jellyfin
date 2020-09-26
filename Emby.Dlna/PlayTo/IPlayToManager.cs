#nullable enable

using System;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo.EventArgs;
using MediaBrowser.Model.Notifications;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Defines the <see cref="IPlayToManager" />.
    /// </summary>
    public interface IPlayToManager
    {
        /// <summary>
        /// An event handler that is triggered on reciept of a PlayTo client subscription event.
        /// </summary>
        event EventHandler<DlnaEventArgs>? DLNAEvents;

        /// <summary>
        /// Gets a value indicating whether gets the current status of DLNA playTo is enabled..
        /// </summary>
        public bool IsPlayToEnabled { get; }

        /// <summary>
        /// Method that triggers a DLNAEvents event.
        /// </summary>
        /// <param name="args">A DlnaEventArgs instance containing the event message.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task NotifyDevice(DlnaEventArgs args);

        /// <summary>
        /// Sends a client notification message.
        /// </summary>
        /// <param name="device">Device sending the notification.</param>
        /// <param name="notification">The notification to send.</param>
        /// <returns>Task.</returns>
        Task SendNotification(PlayToDevice device, NotificationRequest notification);
    }
}
