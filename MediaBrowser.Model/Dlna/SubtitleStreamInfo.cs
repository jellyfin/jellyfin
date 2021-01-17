namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="SubtitleStreamInfo" />.
    /// </summary>
    public class SubtitleStreamInfo
    {
        /// <summary>
        /// Gets or sets the Url.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether these subtitles are forced.
        /// </summary>
        public bool IsForced { get; set; }

        /// <summary>
        /// Gets or sets the Format.
        /// </summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display title.
        /// </summary>
        public string DisplayTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the subtitle delivery method.
        /// </summary>
        public SubtitleDeliveryMethod DeliveryMethod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is an external Url.
        /// </summary>
        public bool IsExternalUrl { get; set; }
    }
}
