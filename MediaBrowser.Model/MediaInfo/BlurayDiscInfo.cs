using MediaBrowser.Model.Entities;
using ProtoBuf;
using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo
{
    /// <summary>
    /// Represents the result of BDInfo output
    /// </summary>
    [ProtoContract]
    public class BlurayDiscInfo
    {
        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        [ProtoMember(1)]
        public List<MediaStream> MediaStreams { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        [ProtoMember(2)]
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>The files.</value>
        [ProtoMember(3)]
        public List<string> Files { get; set; }

        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        [ProtoMember(4)]
        public List<double> Chapters { get; set; }
    }
}
