using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Extensions;
using System.ComponentModel;
using System.Diagnostics;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class RecordingGroupDto.
    /// </summary>
    [DebuggerDisplay("Name = {Name}, Count = {RecordingCount}")]
    public class RecordingGroupDto : IHasPropertyChangedEvent, IHasServerId
    {
        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        /// <value>The server identifier.</value>
        public string ServerId { get; set; }
        
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
