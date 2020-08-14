using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Events
{
    /// <summary>
    /// An interface that handles eventing.
    /// </summary>
    public interface IEventManager
    {
        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <typeparam name="T">The type of event.</typeparam>
        /// <returns>A task representing the publishing of the event.</returns>
        Task Publish<T>(T eventArgs)
            where T : EventArgs;
    }
}
