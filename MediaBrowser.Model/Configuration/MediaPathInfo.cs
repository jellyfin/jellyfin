#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public class MediaPathInfo
    {
        public MediaPathInfo(string path)
        {
            Path = path;
        }

        // Needed for xml serialization
        public MediaPathInfo()
        {
            Path = string.Empty;
        }

        public string Path { get; set; }
    }
}
