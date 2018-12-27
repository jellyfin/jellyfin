using MediaBrowser.Model.Devices;

namespace MediaBrowser.Controller.Devices
{
    public class CameraImageUploadInfo
    {
        public LocalFileInfo FileInfo { get; set; }
        public DeviceInfo Device { get; set; }
    }
}
