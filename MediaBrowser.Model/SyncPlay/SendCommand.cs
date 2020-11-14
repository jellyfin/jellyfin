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
        public SendCommand()
        {
            GroupId = string.Empty;
            PlaylistItemId = string.Empty;
        }

        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets the playlist identifier of the playing item.
        /// </summary>
        /// <value>The playlist identifier of the playing item.</value>
        public string PlaylistItemId { get; set; }

        /// <summary>
        /// Gets or sets the UTC time when to execute the command.
        /// </summary>
        /// <value>The UTC time when to execute the command.</value>
        public DateTime When { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long? PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        /// <value>The command.</value>
        public SendCommandType Command { get; set; }

        /// <summary>
        /// Gets or sets the UTC time when this command has been emitted.
        /// </summary>
        /// <value>The UTC time when this command has been emitted.</value>
        public DateTime EmittedAt { get; set; }
    }
}
