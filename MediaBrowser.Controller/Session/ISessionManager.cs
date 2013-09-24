using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Interface ISessionManager
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="remoteControllers">The remote controllers.</param>
        void AddParts(IEnumerable<ISessionRemoteController> remoteControllers);

        /// <summary>
        /// Occurs when [playback start].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackStart;

        /// <summary>
        /// Occurs when [playback progress].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;

        /// <summary>
        /// Occurs when [playback stopped].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackStopped;

        /// <summary>
        /// Gets the sessions.
        /// </summary>
        /// <value>The sessions.</value>
        IEnumerable<SessionInfo> Sessions { get; }

        /// <summary>
        /// Logs the user activity.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        Task<SessionInfo> LogConnectionActivity(string clientType, string appVersion, string deviceId, string deviceName, User user);

        /// <summary>
        /// Used to report that playback has started for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        Task OnPlaybackStart(PlaybackInfo info);

        /// <summary>
        /// Used to report playback progress for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="isPaused">if set to <c>true</c> [is paused].</param>
        /// <param name="isMuted">if set to <c>true</c> [is muted].</param>
        /// <param name="sessionId">The session id.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task OnPlaybackProgress(BaseItem item, long? positionTicks, bool isPaused, bool isMuted, Guid sessionId);

        /// <summary>
        /// Used to report that playback has ended for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="sessionId">The session id.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task OnPlaybackStopped(BaseItem item, long? positionTicks, Guid sessionId);

        /// <summary>
        /// Sends the system command.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendSystemCommand(Guid sessionId, SystemCommand command, CancellationToken cancellationToken);
    }
}