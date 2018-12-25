
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
