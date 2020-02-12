#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Devices
{
    public class LocalFileInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Album { get; set; }
        public string MimeType { get; set; }
    }
}
