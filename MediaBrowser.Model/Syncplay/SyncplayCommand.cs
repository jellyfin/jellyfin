namespace MediaBrowser.Model.Syncplay
{
    /// <summary>
    /// Class SyncplayCommand.
    /// </summary>
    public class SyncplayCommand
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
        public SyncplayCommandType Command { get; set; }
    }
}
