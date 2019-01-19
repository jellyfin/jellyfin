using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class PhotoAlbum : Folder
    {
        [IgnoreDataMember]
        public override bool AlwaysScanInternalMetadataPath => true;

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus => false;

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages => false;
    }
}
