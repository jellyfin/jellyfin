using System.IO;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public interface ISubtitleParser
    {
        SubtitleInfo Parse(Stream stream);
    }
}
