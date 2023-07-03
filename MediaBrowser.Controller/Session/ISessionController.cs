#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Session;

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
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="name">Name of message type.</param>
        /// <param name="messageId">Message ID.</param>
        /// <param name="data">Data to send.</param>
        /// <param name="cancellationToken">CancellationToken for operation.</param>
        /// <returns>A task.</returns>
        Task SendMessage<T>(SessionMessageType name, Guid messageId, T data, CancellationToken cancellationToken);
    }
}
