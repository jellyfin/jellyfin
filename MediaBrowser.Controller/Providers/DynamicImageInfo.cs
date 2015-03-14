using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class DynamicImageInfo
    {
        public string ImageId { get; set; }
        public ImageType Type { get; set; }
    }
}