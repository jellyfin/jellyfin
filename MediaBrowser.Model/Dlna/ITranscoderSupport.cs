#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ITranscoderSupport"/> interface.
    /// </summary>
    public interface ITranscoderSupport
    {
        bool CanEncodeToAudioCodec(string codec);

        bool CanEncodeToSubtitleCodec(string codec);

        bool CanExtractSubtitles(string codec);
    }
}
