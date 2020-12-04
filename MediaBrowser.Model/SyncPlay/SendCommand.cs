using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class SendCommand.
    /// </summary>
    public class SendCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendCommand"/> class.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="playlistItemId">The playlist identifier of the playing item.</param>
        /// <param name="when">The UTC time when to execute the command.</param>
        /// <param name="command">The command.</param>
        /// <param name="positionTicks">The position ticks, for commands that require it.</param>
        /// <param name="emittedAt">The UTC time when this command has been emitted.</param>
        public SendCommand(Guid groupId, Guid playlistItemId, DateTime when, SendCommandType command, long? positionTicks, DateTime emittedAt)
        {
            GroupId = groupId;
            PlaylistItemId = playlistItemId;
            When = when;
            Command = command;
            PositionTicks = positionTicks;
            EmittedAt = emittedAt;
        }

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public Guid GroupId { get; }

        /// <summary>
        /// Gets the playlist identifier of the playing item.
        /// </summary>
        /// <value>The playlist identifier of the playing item.</value>
        public Guid PlaylistItemId { get; }

        /// <summary>
        /// Gets or sets the UTC time when to execute the command.
        /// </summary>
        /// <value>The UTC time when to execute the command.</value>
        public DateTime When { get; set; }

        /// <summary>
        /// Gets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long? PositionTicks { get; }

        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <value>The command.</value>
        public SendCommandType Command { get; }

        /// <summary>
        /// Gets the UTC time when this command has been emitted.
        /// </summary>
        /// <value>The UTC time when this command has been emitted.</value>
        public DateTime EmittedAt { get; }
    }
}
