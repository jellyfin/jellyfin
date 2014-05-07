using System.IO;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public interface ISubtitleParser
    {
        SubtitleTrackInfo Parse(Stream stream);
    }
}
