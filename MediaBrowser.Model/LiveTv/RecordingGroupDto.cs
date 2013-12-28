
namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class RecordingGroupDto.
    /// </summary>
    public class RecordingGroupDto
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the recording count.
        /// </summary>
        /// <value>The recording count.</value>
        public int RecordingCount { get; set; }
    }
}
