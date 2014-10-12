using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Controller.Entities
{
    public class ItemImageInfo
    {
        public string Path { get; set; }

        public ImageType Type { get; set; }

        public DateTime DateModified { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }
    }
}
