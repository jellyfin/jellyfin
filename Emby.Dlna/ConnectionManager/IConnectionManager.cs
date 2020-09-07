#pragma warning disable CS1591

using Emby.Dlna.Eventing;
using Emby.Dlna.Service;

namespace Emby.Dlna.ConnectionManager
{
    public interface IConnectionManager : IDlnaEventManager, IUpnpService
    {
    }
}
