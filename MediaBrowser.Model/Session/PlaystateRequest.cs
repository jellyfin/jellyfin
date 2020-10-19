#pragma warning disable CS1591

namespace MediaBrowser.Model.Session
{
    public class PlaystateRequest
    {
        public PlaystateCommand Command { get; set; }

        public long? SeekPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the controlling user identifier.
        /// </summary>
        /// <value>The controlling user identifier.</value>
        public string? ControllingUserId { get; set; }
    }
}
