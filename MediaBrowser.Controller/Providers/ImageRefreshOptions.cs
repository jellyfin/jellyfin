using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class ImageRefreshOptions
    {
        public ImageRefreshMode ImageRefreshMode { get; set; }
        public IDirectoryService DirectoryService { get; private set; }

        public bool ReplaceAllImages { get; set; }

        public List<ImageType> ReplaceImages { get; set; }
        public bool IsAutomated { get; set; }

        public ImageRefreshOptions(IDirectoryService directoryService)
        {
            ImageRefreshMode = ImageRefreshMode.Default;
            DirectoryService = directoryService;

            ReplaceImages = new List<ImageType>();
            IsAutomated = true;
        }

        public bool IsReplacingImage(ImageType type)
        {
            return ImageRefreshMode == ImageRefreshMode.FullRefresh &&
                   (ReplaceAllImages || ReplaceImages.Contains(type));
        }
    }
}