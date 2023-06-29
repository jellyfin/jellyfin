#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public interface ITranscoderSupport
    {
        bool CanEncodeToAudioCodec(string codec);

        bool CanEncodeToSubtitleCodec(string codec);

        bool CanExtractSubtitles(string codec);
    }
}
