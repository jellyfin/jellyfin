namespace MediaBrowser.Model.Dlna
{
    public interface ITranscoderSupport
    {
        bool CanEncodeToAudioCodec(string codec);
    }

    public class FullTranscoderSupport : ITranscoderSupport
    {
        public bool CanEncodeToAudioCodec(string codec)
        {
            return true;
        }
    }
}
