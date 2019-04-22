using System.Collections.Generic;
using Jellyfin.Controller.Entities;

namespace Jellyfin.Controller.Providers
{
    public interface ILocalImageFileProvider : ILocalImageProvider
    {
        List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService);
    }
}
