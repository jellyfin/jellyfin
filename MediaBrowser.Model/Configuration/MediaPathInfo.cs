#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public class MediaPathInfo
    {
        public MediaPathInfo(string path)
        {
            Path = path;
        }

        public string Path { get; set; }

        public string? NetworkPath { get; set; }
    }
}
