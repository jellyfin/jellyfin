using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using System;

namespace MediaBrowser.Controller.MediaEncoding
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
