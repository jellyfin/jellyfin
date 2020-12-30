namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="FullTranscoderSupport" />.
    /// </summary>
    public class FullTranscoderSupport : ITranscoderSupport
    {
        /// <inheritdoc/>
        public bool CanEncodeToAudioCodec(string codec)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool CanEncodeToSubtitleCodec(string codec)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool CanExtractSubtitles(string codec)
        {
            return true;
        }
    }
}
