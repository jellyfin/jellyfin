using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class ExtraInfo
    {
        public string Path { get; set; }

        public LocationType LocationType { get; set; }

        public bool IsDownloadable { get; set; }

        public ExtraType ExtraType { get; set; }
    }
}