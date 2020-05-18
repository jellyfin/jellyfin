#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;

namespace MediaBrowser.Model.Devices
{
    public class DevicesOptions
    {
        public DevicesOptions()
        {
            EnabledCameraUploadDevices = Array.Empty<string>();
        }

        public string[] EnabledCameraUploadDevices { get; set; }

        public string CameraUploadPath { get; set; }

        public bool EnableCameraUploadSubfolders { get; set; }
    }
}
