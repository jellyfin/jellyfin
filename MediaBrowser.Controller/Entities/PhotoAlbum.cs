using MediaBrowser.Model.Configuration;
using System.Linq;

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

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Other);
        }
    }
}
