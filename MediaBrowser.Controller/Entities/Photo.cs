using MediaBrowser.Model.Drawing;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class Photo : BaseItem, IHasTags, IHasTaglines
    {
        public List<string> Tags { get; set; }
        public List<string> Taglines { get; set; }

        public Photo()
        {
            Tags = new List<string>();
            Taglines = new List<string>();
        }

        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Photo;
            }
        }

        public int? Width { get; set; }
        public int? Height { get; set; }
        public string CameraManufacturer { get; set; }
        public string CameraModel { get; set; }
        public string Software { get; set; }
        public double? ExposureTime { get; set; }
        public double? FocalLength { get; set; }

        public ImageOrientation? Orientation { get; set; }

        public double? Aperture { get; set; }
        public double? ShutterSpeed { get; set; }
    }
}
