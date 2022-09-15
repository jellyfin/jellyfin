namespace MediaBrowser.Controller.Lyrics
{
    /// <summary>
    /// Lyric dto.
    /// </summary>
    public class Lyric
    {
        /// <summary>
        /// Gets or sets the start time (ticks).
        /// </summary>
        public double? Start { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
