using Jellyfin.Model.Entities;

namespace Jellyfin.Controller.Providers
{
    public class DynamicImageInfo
    {
        public string ImageId { get; set; }
        public ImageType Type { get; set; }
    }
}
