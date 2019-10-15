using System.Text.Json.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class PhotoAlbum : Folder
    {
        [JsonIgnore]
        public override bool AlwaysScanInternalMetadataPath => true;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => false;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;
    }
}
