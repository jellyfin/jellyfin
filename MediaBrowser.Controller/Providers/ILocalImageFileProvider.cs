using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers
{
    public interface ILocalImageFileProvider : ILocalImageProvider
    {
        List<LocalImageInfo> GetImages(IHasImages item, IDirectoryService directoryService);
    }
}