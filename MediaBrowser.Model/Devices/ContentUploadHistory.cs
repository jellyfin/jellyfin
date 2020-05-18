#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;

namespace MediaBrowser.Model.Devices
{
    public class ContentUploadHistory
    {
        public ContentUploadHistory()
        {
            FilesUploaded = Array.Empty<LocalFileInfo>();
        }

        public string DeviceId { get; set; }

        public LocalFileInfo[] FilesUploaded { get; set; }
    }
}
