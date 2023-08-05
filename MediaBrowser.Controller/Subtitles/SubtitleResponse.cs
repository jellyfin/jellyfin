#nullable disable

#pragma warning disable CS1591

using System.IO;

namespace MediaBrowser.Controller.Subtitles
{
    public class SubtitleResponse
    {
        public string Language { get; set; }

        public string Format { get; set; }

        public bool IsForced { get; set; }

        public bool IsHearingImpaired { get; set; }

        public Stream Stream { get; set; }
    }
}
