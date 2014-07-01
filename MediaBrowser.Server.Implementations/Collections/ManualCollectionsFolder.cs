using MediaBrowser.Controller.Entities;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Collections
{
    public class ManualCollectionsFolder : BasePluginFolder
    {
        public ManualCollectionsFolder()
        {
            Name = "Collections";
        }

        public override bool IsVisible(User user)
        {
            if (!GetChildren(user, true).Any())
            {
                return false;
            }

            return base.IsVisible(user);
        }

        public override bool IsHidden
        {
            get
            {
                return true;
            }
        }

        public override bool IsHiddenFromUser(User user)
        {
            return true;
        }

        public override string CollectionType
        {
            get { return Model.Entities.CollectionType.BoxSets; }
        }

        public override string GetClientTypeName()
        {
            return typeof (CollectionFolder).Name;
        }
    }
}