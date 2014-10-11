
namespace MediaBrowser.Model.Devices
{
    public class DevicesOptions
    {
        public string[] EnabledCameraUploadDevices { get; set; }
        public string CameraUploadPath { get; set; }

        public DevicesOptions()
        {
            EnabledCameraUploadDevices = new string[] { };
        }
    }
}
