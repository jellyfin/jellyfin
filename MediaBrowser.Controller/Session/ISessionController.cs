using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Session
{
    public interface ISessionController
    {
        /// <summary>
        /// Gets a value indicating whether this instance is session active.
        /// </summary>
        /// <value><c>true</c> if this instance is session active; otherwise, <c>false</c>.</value>
        bool IsSessionActive { get; }

        /// <summary>
        /// Gets a value indicating whether [supports media remote control].
        /// </summary>
        /// <value><c>true</c> if [supports media remote control]; otherwise, <c>false</c>.</value>
        bool SupportsMediaControl { get; }

        /// <summary>
        /// Sends the message.
        /// </summary>
        Task SendMessage<T>(string name, Guid messageId, T data, CancellationToken cancellationToken);
    }
}
