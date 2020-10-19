#nullable disable
#pragma warning disable CS1591

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.MediaInfo
{
    /// <summary>
    /// Represents the result of BDInfo output.
    /// </summary>
    public class BlurayDiscInfo
    {
        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        public MediaStream[] MediaStreams { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>The files.</value>
        public string[] Files { get; set; }

        public string PlaylistName { get; set; }

        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        public double[] Chapters { get; set; }
    }
}
