using Emby.Dlna.Eventing;
using Emby.Dlna.Service;

namespace Emby.Dlna.ConnectionManager
{
    /// <summary>
    /// Interface class for <seealso cref="ConnectionManagerService"/> class.
    /// </summary>
    public interface IConnectionManager : IDlnaEventManager, IUpnpService
    {
    }
}
