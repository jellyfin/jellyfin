#nullable disable

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class SendCommand.
    /// </summary>
    public class SendCommand
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets the UTC time when to execute the command.
        /// </summary>
        /// <value>The UTC time when to execute the command.</value>
        public string When { get; set; }

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
        public string EmittedAt { get; set; }
    }
}
