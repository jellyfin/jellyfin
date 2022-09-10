namespace Jellyfin.Api.Models.UserDtos
{
    /// <summary>
    /// Lyric dto.
    /// </summary>
    public class Lyrics
    {
        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        public double? Start { get; set; }

        /// <summary>
        /// Gets or sets the test.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public string? Error { get; set; }
    }
}
