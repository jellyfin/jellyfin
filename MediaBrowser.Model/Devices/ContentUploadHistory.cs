#pragma warning disable CS1591
#pragma warning disable SA1600

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
