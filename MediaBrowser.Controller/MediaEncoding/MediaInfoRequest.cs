#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class MediaInfoRequest
    {
        public MediaSourceInfo MediaSource { get; set; }

        public bool ExtractChapters { get; set; }

        public DlnaProfileType MediaType { get; set; }
    }
}
