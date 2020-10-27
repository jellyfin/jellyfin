namespace MediaBrowser.Model.Dlna
{
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