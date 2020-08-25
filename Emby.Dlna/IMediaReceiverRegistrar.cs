#pragma warning disable CS1591

using Emby.Dlna.Eventing;

namespace Emby.Dlna
{
    public interface IMediaReceiverRegistrar : IEventManager, IUpnpService
    {
    }
}
