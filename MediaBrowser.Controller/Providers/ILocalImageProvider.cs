using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// This is just a marker interface.
    /// </summary>
    public interface ILocalImageProvider : IImageProvider
    {
        List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService);
    }
}
