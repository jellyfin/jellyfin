
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Plugins derive from and export this class to create a folder that will appear in the root along
    /// with all the other actual physical folders in the system.
    /// </summary>
    public abstract class BasePluginFolder : Folder, ICollectionFolder
    {
        public virtual string CollectionType
        {
            get { return null; }
        }

        public override bool CanDelete()
        {
            return false;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get
            {
                return false;
            }
        }
    }
}
