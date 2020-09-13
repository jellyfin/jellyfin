using Emby.Dlna.Eventing;
using Emby.Dlna.Service;

namespace Emby.Dlna.ContentDirectory
{
    /// <summary>
    /// Interface for <see cref="ContentDirectoryService"/> class.
    /// </summary>
    public interface IContentDirectory : IDlnaEventManager, IUpnpService
    {
    }
}
