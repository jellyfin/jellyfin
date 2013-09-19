
namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class SessionQuery
    /// </summary>
    public class SessionQuery
    {
        /// <summary>
        /// Filter by sessions that are allowed to be controlled by a given user
        /// </summary>
        public string ControllableByUserId { get; set; }

        /// <summary>
        /// Filter by sessions that either do or do not support remote control. Default returns all sessions.
        /// </summary>
        public bool? SupportsRemoteControl { get; set; }
    }
}
