
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class ImageDownloadOptions
    /// </summary>
    public class ImageDownloadOptions
    {
        /// <summary>
        /// Download Art Image
        /// </summary>
        /// <value><c>true</c> if art; otherwise, <c>false</c>.</value>
        public bool Art { get; set; }

        /// <summary>
        /// Download Logo Image
        /// </summary>
        /// <value><c>true</c> if logo; otherwise, <c>false</c>.</value>
        public bool Logo { get; set; }

        /// <summary>
        /// Download Primary Image
        /// </summary>
        /// <value><c>true</c> if primary; otherwise, <c>false</c>.</value>
        public bool Primary { get; set; }

        /// <summary>
        /// Download Backdrop Images
        /// </summary>
        /// <value><c>true</c> if backdrops; otherwise, <c>false</c>.</value>
        public bool Backdrops { get; set; }

        /// <summary>
        /// Download Disc Image
        /// </summary>
        /// <value><c>true</c> if disc; otherwise, <c>false</c>.</value>
        public bool Disc { get; set; }

        /// <summary>
        /// Download Thumb Image
        /// </summary>
        /// <value><c>true</c> if thumb; otherwise, <c>false</c>.</value>
        public bool Thumb { get; set; }

        /// <summary>
        /// Download Banner Image
        /// </summary>
        /// <value><c>true</c> if banner; otherwise, <c>false</c>.</value>
        public bool Banner { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDownloadOptions"/> class.
        /// </summary>
        public ImageDownloadOptions()
        {
            Art = true;
            Logo = true;
            Primary = true;
            Backdrops = true;
            Disc = true;
            Thumb = true;
            Banner = true;
        }
    }
}
