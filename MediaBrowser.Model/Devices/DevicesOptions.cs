using System;

namespace MediaBrowser.Model.Devices
{
    public class DevicesOptions
    {
        public string[] EnabledCameraUploadDevices { get; set; }
        public string CameraUploadPath { get; set; }
        public bool EnableCameraUploadSubfolders { get; set; }

        public DevicesOptions()
        {
            EnabledCameraUploadDevices = Array.Empty<string>();
        }
    }

    public class DeviceOptions
    {
        public string CustomName { get; set; }
    }
}
