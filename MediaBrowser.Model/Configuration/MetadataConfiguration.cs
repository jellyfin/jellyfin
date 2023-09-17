#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public class MetadataConfiguration
    {
        public MetadataConfiguration()
        {
            UseFileCreationTimeForDateAdded = false;
        }

        public bool UseFileCreationTimeForDateAdded { get; set; }
    }
}
