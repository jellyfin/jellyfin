using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Events
{
    /// <summary>
    /// An interface representing a type that consumes events of type <c>T</c>.
    /// </summary>
    /// <typeparam name="T">The type of events this consumes.</typeparam>
    public interface IEventConsumer<in T>
        where T : EventArgs
    {
        /// <summary>
        /// A method that is called when an event of type <c>T</c> is fired.
        /// </summary>
        /// <param name="eventArgs">The event.</param>
        /// <returns>A task representing the consumption of the event.</returns>
        Task OnEvent(T eventArgs);
    }
}
