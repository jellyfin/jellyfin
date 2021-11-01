#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvTunerInfo
    {
        public LiveTvTunerInfo()
        {
            Clients = new List<string>();
        }

        /// <summary>
        /// Gets or sets the type of the source.
        /// </summary>
        /// <value>The type of the source.</value>
        public string SourceType { get; set; }

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
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public LiveTvTunerStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the recording identifier.
        /// </summary>
        /// <value>The recording identifier.</value>
        public string RecordingId { get; set; }

        /// <summary>
        /// Gets or sets the name of the program.
        /// </summary>
        /// <value>The name of the program.</value>
        public string ProgramName { get; set; }

        /// <summary>
        /// Gets or sets the clients.
        /// </summary>
        /// <value>The clients.</value>
        public List<string> Clients { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can reset.
        /// </summary>
        /// <value><c>true</c> if this instance can reset; otherwise, <c>false</c>.</value>
        public bool CanReset { get; set; }
    }
}
