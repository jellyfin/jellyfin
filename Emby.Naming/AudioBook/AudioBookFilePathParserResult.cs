namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Data object for passing result of audiobook part/chapter extraction.
    /// </summary>
    public record struct AudioBookFilePathParserResult
    {
        /// <summary>
        /// Gets or sets optional number of path extracted from audiobook filename.
        /// </summary>
        public int? PartNumber { get; set; }

        /// <summary>
        /// Gets or sets optional number of chapter extracted from audiobook filename.
        /// </summary>
        public int? ChapterNumber { get; set; }
    }
}
