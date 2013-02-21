using MediaBrowser.Controller.Entities;
using System;

namespace MediaBrowser.Plugins.Trailers
{
    /// <summary>
    /// This is a stub class used to hold information about a trailer
    /// </summary>
    public class TrailerInfo
    {
        /// <summary>
        /// Gets or sets the video.
        /// </summary>
        /// <value>The video.</value>
        public Trailer Video { get; set; }
        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>The image URL.</value>
        public string ImageUrl { get; set; }
        /// <summary>
        /// Gets or sets the hd image URL.
        /// </summary>
        /// <value>The hd image URL.</value>
        public string HdImageUrl { get; set; }
        /// <summary>
        /// Gets or sets the trailer URL.
        /// </summary>
        /// <value>The trailer URL.</value>
        public string TrailerUrl { get; set; }
        /// <summary>
        /// Gets or sets the post date.
        /// </summary>
        /// <value>The post date.</value>
        public DateTime PostDate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailerInfo" /> class.
        /// </summary>
        public TrailerInfo()
        {
            Video = new Trailer();
        }
    }
}
