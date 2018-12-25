using System.Collections.Generic;

namespace MediaBrowser.Model.Devices
{
    public class ContentUploadHistory
    {
        public string DeviceId { get; set; }
        public LocalFileInfo[] FilesUploaded { get; set; }

        public ContentUploadHistory()
        {
            FilesUploaded = new LocalFileInfo[] { };
        }
    }
}
