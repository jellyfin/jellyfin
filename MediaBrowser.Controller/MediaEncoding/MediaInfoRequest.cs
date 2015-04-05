using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class MediaInfoRequest
    {
        public string InputPath { get; set; }
        public MediaProtocol Protocol { get; set; }
        public bool ExtractChapters { get; set; }
        public DlnaProfileType MediaType { get; set; }
        public IIsoMount MountedIso { get; set; }
        public VideoType VideoType { get; set; }
        public List<string> PlayableStreamFileNames { get; set; }

        public MediaInfoRequest()
        {
            PlayableStreamFileNames = new List<string>();
        }
    }
}
