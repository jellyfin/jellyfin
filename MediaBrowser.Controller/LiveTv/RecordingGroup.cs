using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.LiveTv
{
    public class RecordingGroup : Folder
    {
        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            // Don't block. 
            return false;
        }

        public override bool SupportsLocalMetadata
        {
            get
            {
                return false;
            }
        }
    }
}
