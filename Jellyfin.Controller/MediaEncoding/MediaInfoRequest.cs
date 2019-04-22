using System;
using Jellyfin.Model.Dlna;
using Jellyfin.Model.Dto;
using Jellyfin.Model.IO;

namespace Jellyfin.Controller.MediaEncoding
{
    public class MediaInfoRequest
    {
        public MediaSourceInfo MediaSource { get; set; }
        public bool ExtractChapters { get; set; }
        public DlnaProfileType MediaType { get; set; }
        public IIsoMount MountedIso { get; set; }
        public string[] PlayableStreamFileNames { get; set; }

        public MediaInfoRequest()
        {
            PlayableStreamFileNames = Array.Empty<string>();
        }
    }
}
