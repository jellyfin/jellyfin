using MediaBrowser.Model.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Entities
{
    public class PhotoAlbum : Folder
    {
        public override bool SupportsLocalMetadata
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override bool AlwaysScanInternalMetadataPath
        {
            get
            {
                return true;
            }
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Other);
        }
    }
}
