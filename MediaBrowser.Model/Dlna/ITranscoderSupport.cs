namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ITranscoderSupport" />.
    /// </summary>
    public interface ITranscoderSupport
    {
        /// <summary>
        /// Checks to see if <paramref name="codec"/> can be encoded to audio codec.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>True if <paramref name="codec"/> can be encoded to audio codec.</returns>
        bool CanEncodeToAudioCodec(string codec);

        /// <summary>
        /// Checks to see if <paramref name="codec"/> can be encoded to a subtitle codec.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>True if the <paramref name="codec"/> can be encoded to a subtitle codec.</returns>
        bool CanEncodeToSubtitleCodec(string codec);

        /// <summary>
        /// Checks to see if the subtitles can be extracted from <paramref name="codec"/>.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>True if the subtitles can be extracted from <paramref name="codec"/>.</returns>
        bool CanExtractSubtitles(string codec);
    }
}
