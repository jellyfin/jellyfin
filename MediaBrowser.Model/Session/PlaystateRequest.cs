namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// A request to change the playstate of a session.
    /// </summary>
    public class PlaystateRequest
    {
        /// <summary>
        /// Gets or sets the playstate command.
        /// </summary>
        public PlaystateCommand Command { get; set; }

        /// <summary>
        /// Gets or sets the seek position in ticks.
        /// </summary>
        public long? SeekPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the controlling user identifier.
        /// </summary>
        /// <value>The controlling user identifier.</value>
        public string? ControllingUserId { get; set; }
    }
}
