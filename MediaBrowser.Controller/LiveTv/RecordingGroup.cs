using System.Runtime.Serialization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.LiveTv
{
    public class RecordingGroup : Folder
    {
        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            // Don't block. 
            return false;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.LiveTvProgram;
        }

        public override bool SupportsLocalMetadata
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override SourceType SourceType
        {
            get { return SourceType.LiveTV; }
            set { }
        }
    }
}
