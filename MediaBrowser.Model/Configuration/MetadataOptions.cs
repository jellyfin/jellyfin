using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using System.Collections.Generic;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class MetadataOptions.
    /// </summary>
    public class MetadataOptions
    {
        public string ItemType { get; set; }

        public ImageOption[] ImageOptions { get; set; }

        public string[] DisabledMetadataSavers { get; set; }
        public string[] LocalMetadataReaderOrder { get; set; }

        public string[] DisabledMetadataFetchers { get; set; }
        public string[] MetadataFetcherOrder { get; set; }

        public string[] DisabledImageFetchers { get; set; }
        public string[] ImageFetcherOrder { get; set; }
        
        public MetadataOptions()
            : this(3, 1280)
        {
        }

        public MetadataOptions(int backdropLimit, int minBackdropWidth)
        {
            List<ImageOption> imageOptions = new List<ImageOption>
            {
                new ImageOption
                {
                    Limit = backdropLimit,
                    MinWidth = minBackdropWidth,
                    Type = ImageType.Backdrop
                }
            };

            ImageOptions = imageOptions.ToArray();
            DisabledMetadataSavers = new string[] { };
            LocalMetadataReaderOrder = new string[] { };

            DisabledMetadataFetchers = new string[] { };
            MetadataFetcherOrder = new string[] { };
            DisabledImageFetchers = new string[] { };
            ImageFetcherOrder = new string[] { };
        }

        public int GetLimit(ImageType type)
        {
            ImageOption option = null;
            foreach (ImageOption i in ImageOptions)
            {
                if (i.Type == type)
                {
                    option = i;
                    break;
                }
            }

            return option == null ? 1 : option.Limit;
        }

        public int GetMinWidth(ImageType type)
        {
            ImageOption option = null;
            foreach (ImageOption i in ImageOptions)
            {
                if (i.Type == type)
                {
                    option = i;
                    break;
                }
            }

            return option == null ? 0 : option.MinWidth;
        }

        public bool IsEnabled(ImageType type)
        {
            return GetLimit(type) > 0;
        }

        public bool IsMetadataSaverEnabled(string name)
        {
            return !ListHelper.ContainsIgnoreCase(DisabledMetadataSavers, name);
        }
    }
}
