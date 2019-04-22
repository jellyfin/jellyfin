using Jellyfin.Model.Devices;

namespace Jellyfin.Controller.Devices
{
    public class CameraImageUploadInfo
    {
        public LocalFileInfo FileInfo { get; set; }
        public DeviceInfo Device { get; set; }
    }
}
