namespace MediaBrowser.Model.Dto
{
    public class SubtitleDownloadOptions
    {
        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the media source identifier.
        /// </summary>
        /// <value>The media source identifier.</value>
        public string MediaSourceId { get; set; }
        
        /// <summary>
        /// Gets or sets the index of the stream.
        /// </summary>
        /// <value>The index of the stream.</value>
        public int StreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        public string Format { get; set; }
    }
}