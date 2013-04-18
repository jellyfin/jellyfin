
namespace MediaBrowser.Model.Entities
{
    public class ImageDownloadOptions
    {
        /// <summary>
        /// Download Art Image
        /// </summary>
        public bool Art { get; set; }

        /// <summary>
        /// Download Logo Image
        /// </summary>
        public bool Logo { get; set; }

        /// <summary>
        /// Download Primary Image
        /// </summary>
        public bool Primary { get; set; }

        /// <summary>
        /// Download Backdrop Images
        /// </summary>
        public bool Backdrops { get; set; }

        /// <summary>
        /// Download Disc Image
        /// </summary>
        public bool Disc { get; set; }

        /// <summary>
        /// Download Thumb Image
        /// </summary>
        public bool Thumb { get; set; }

        /// <summary>
        /// Download Banner Image
        /// </summary>
        public bool Banner { get; set; }

    }
}
