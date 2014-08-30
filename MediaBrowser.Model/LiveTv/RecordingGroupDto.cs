using System.ComponentModel;
using System.Diagnostics;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class RecordingGroupDto.
    /// </summary>
    [DebuggerDisplay("Name = {Name}, Count = {RecordingCount}")]
    public class RecordingGroupDto : IHasPropertyChangedEvent
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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
