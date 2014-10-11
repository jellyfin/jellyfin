using System.Collections.Generic;

namespace MediaBrowser.Model.Devices
{
    public class ContentUploadHistory
    {
        public string DeviceId { get; set; }
        public List<LocalFileInfo> FilesUploaded { get; set; }

        public ContentUploadHistory()
        {
            FilesUploaded = new List<LocalFileInfo>();
        }
    }
}
