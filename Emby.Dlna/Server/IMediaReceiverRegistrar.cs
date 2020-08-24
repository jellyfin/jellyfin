#pragma warning disable CS1591

using Emby.Dlna.Eventing;

namespace Emby.Dlna.Server
{
    public interface IMediaReceiverRegistrar : IEventManager, IUpnpService
    {
    }
}
