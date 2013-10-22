using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is essentially a marker interface
    /// </summary>
    public interface IHasMediaStreams
    {
        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        List<MediaStream> MediaStreams { get; set; }
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        string Path { get; set; }
        /// <summary>
        /// Gets or sets the primary image path.
        /// </summary>
        /// <value>The primary image path.</value>
        string PrimaryImagePath { get; }
    }
}
