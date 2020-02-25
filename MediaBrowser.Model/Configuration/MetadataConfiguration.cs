#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Configuration
{
    public class MetadataConfiguration
    {
        public bool UseFileCreationTimeForDateAdded { get; set; }

        public MetadataConfiguration()
        {
            UseFileCreationTimeForDateAdded = true;
        }
    }
}
