using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface ISyncPlayStateContext.
    /// </summary>
    public interface ISyncPlayStateContext
    {
        /// <summary>
        /// Gets the context's group.
        /// </summary>
        /// <value>The group.</value>
        GroupInfo GetGroup();

        /// <summary>
        /// Sets a new state.
        /// </summary>
        /// <param name="state">The new state.</param>
        void SetState(ISyncPlayState state);

        /// <summary>
        /// Sends a GroupUpdate message to the interested sessions.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The task.</value>
        Task SendGroupUpdate<T>(SessionInfo from, SyncPlayBroadcastType type, GroupUpdate<T> message, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a playback command to the interested sessions.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The task.</value>
        Task SendCommand(SessionInfo from, SyncPlayBroadcastType type, SendCommand message, CancellationToken cancellationToken);

        /// <summary>
        /// Builds a new playback command with some default values.
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <value>The SendCommand.</value>
        SendCommand NewSyncPlayCommand(SendCommandType type);

        /// <summary>
        /// Builds a new group update message.
        /// </summary>
        /// <param name="type">The update type.</param>
        /// <param name="data">The data to send.</param>
        /// <value>The GroupUpdate.</value>
        GroupUpdate<T> NewSyncPlayGroupUpdate<T>(GroupUpdateType type, T data);

        /// <summary>
        /// Converts DateTime to UTC string.
        /// </summary>
        /// <param name="date">The date to convert.</param>
        /// <value>The UTC string.</value>
        string DateToUTCString(DateTime date);

        /// <summary>
        /// Sanitizes the PositionTicks, considers the current playing item when available.
        /// </summary>
        /// <param name="positionTicks">The PositionTicks.</param>
        /// <value>The sanitized PositionTicks.</value>
        long SanitizePositionTicks(long? positionTicks);
    }
}
