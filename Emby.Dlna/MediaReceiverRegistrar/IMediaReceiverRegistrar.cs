using Emby.Dlna.Eventing;
using Emby.Dlna.Service;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    /// <summary>
    /// Interface for <see cref="MediaReceiverRegistrarService"/> class.
    /// </summary>
    public interface IMediaReceiverRegistrar : IDlnaEventManager, IUpnpService
    {
    }
}
