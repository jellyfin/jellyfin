using MediaBrowser.Model.Session;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Session
{
    public interface ISessionController
    {
        /// <summary>
        /// Supportses the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool Supports(SessionInfo session);

        /// <summary>
        /// Sends the system command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendSystemCommand(SessionInfo session, SystemCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageCommand(SessionInfo session, MessageCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the play command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlayCommand(SessionInfo session, PlayRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the browse command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendBrowseCommand(SessionInfo session, BrowseRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the playstate command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlaystateCommand(SessionInfo session, PlaystateRequest command, CancellationToken cancellationToken);
    }
}
