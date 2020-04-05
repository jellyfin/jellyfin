#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Devices
{
    public class ContentUploadHistory
    {
        public string DeviceId { get; set; }

        public LocalFileInfo[] FilesUploaded { get; set; }

        public ContentUploadHistory()
        {
            FilesUploaded = Array.Empty<LocalFileInfo>();
        }
    }
}
