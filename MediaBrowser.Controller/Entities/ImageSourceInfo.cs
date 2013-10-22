using System;

namespace MediaBrowser.Controller.Entities
{
    public class ImageSourceInfo
    {
        public Guid ImagePathMD5 { get; set; }
        public Guid ImageUrlMD5 { get; set; }
    }
}
