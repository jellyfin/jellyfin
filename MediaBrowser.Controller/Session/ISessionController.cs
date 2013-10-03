using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Session
{
    public interface ISessionController
    {
        /// <summary>
        /// Gets a value indicating whether [supports media remote control].
        /// </summary>
        /// <value><c>true</c> if [supports media remote control]; otherwise, <c>false</c>.</value>
        bool SupportsMediaRemoteControl { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is session active.
        /// </summary>
        /// <value><c>true</c> if this instance is session active; otherwise, <c>false</c>.</value>
        bool IsSessionActive { get; }

        /// <summary>
        /// Sends the system command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendSystemCommand(SystemCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageCommand(MessageCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the play command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the browse command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendBrowseCommand(BrowseRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the playstate command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the library update info.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendRestartRequiredNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the user data change info.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the server shutdown notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendServerShutdownNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the server restart notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendServerRestartNotification(CancellationToken cancellationToken);
    }
}
