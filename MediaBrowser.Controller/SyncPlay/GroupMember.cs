using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class GroupMember.
    /// </summary>
    public class GroupMember
    {
        /// <summary>
        /// Gets or sets whether this member is buffering.
        /// </summary>
        /// <value><c>true</c> if member is buffering; <c>false</c> otherwise.</value>
        public bool IsBuffering { get; set; }

        /// <summary>
        /// Gets or sets the session.
        /// </summary>
        /// <value>The session.</value>
        public SessionInfo Session { get; set; }

        /// <summary>
        /// Gets or sets the ping.
        /// </summary>
        /// <value>The ping.</value>
        public long Ping { get; set; }
    }
}
