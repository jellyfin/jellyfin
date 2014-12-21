using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dto
{
    public class DtoOptions
    {
        public List<ItemFields> Fields { get; set; }
        public List<ImageType> ImageTypes { get; set; }
        public int ImageTypeLimit { get; set; }
        public bool EnableImages { get; set; }
        public bool EnableSettings { get; set; }

        public DtoOptions()
        {
            Fields = new List<ItemFields>();
            ImageTypes = new List<ImageType>();
            ImageTypeLimit = int.MaxValue;
            EnableImages = true;
        }

        public int GetImageLimit(ImageType type)
        {
            if (EnableImages && ImageTypes.Contains(type))
            {
                return ImageTypeLimit;
            }

            return 0;
        }
    }
}
