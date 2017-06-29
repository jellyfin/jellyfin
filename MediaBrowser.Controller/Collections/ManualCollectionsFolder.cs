using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Collections
{
    public class ManualCollectionsFolder : BasePluginFolder, IHiddenFromDisplay
    {
        public ManualCollectionsFolder()
        {
            Name = "Collections";
            DisplayMediaType = "CollectionFolder";
        }

        public override bool IsHidden
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages
        {
            get
            {
                return false;
            }
        }

        public bool IsHiddenFromUser(User user)
        {
            return !ConfigurationManager.Configuration.DisplayCollectionsView;
        }

        [IgnoreDataMember]
        public override string CollectionType
        {
            get { return Model.Entities.CollectionType.BoxSets; }
        }

        public override string GetClientTypeName()
        {
            return typeof(CollectionFolder).Name;
        }
    }
}