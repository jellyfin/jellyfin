using System.Linq;
using MediaBrowser.Controller.Entities;

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
                return !ActualChildren.Any() || base.IsHidden;
            }
        }
    }
}