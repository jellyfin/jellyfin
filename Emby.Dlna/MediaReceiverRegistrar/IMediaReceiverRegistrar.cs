#pragma warning disable CS1591

using Emby.Dlna.Eventing;
using Emby.Dlna.Service;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public interface IMediaReceiverRegistrar : IDlnaEventManager, IUpnpService
    {
    }
}
