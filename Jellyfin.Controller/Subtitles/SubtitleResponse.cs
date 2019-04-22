using System.IO;

namespace Jellyfin.Controller.Subtitles
{
    public class SubtitleResponse
    {
        public string Language { get; set; }
        public string Format { get; set; }
        public bool IsForced { get; set; }
        public Stream Stream { get; set; }
    }
}
