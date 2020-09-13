#pragma warning disable CS1591

using Emby.Dlna.Eventing;
using Emby.Dlna.Service;

namespace Emby.Dlna.ContentDirectory
{
    public interface IContentDirectory : IDlnaEventManager, IUpnpService
    {
    }
}
