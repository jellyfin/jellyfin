#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public interface ITranscoderSupport
    {
        bool CanEncodeToAudioCodec(string codec);

        bool CanEncodeToSubtitleCodec(string codec);

        bool CanExtractSubtitles(string codec);
    }

    public class FullTranscoderSupport : ITranscoderSupport
    {
        public bool CanEncodeToAudioCodec(string codec)
        {
            return true;
        }

        public bool CanEncodeToSubtitleCodec(string codec)
        {
            return true;
        }

        public bool CanExtractSubtitles(string codec)
        {
            return true;
        }
    }
}
