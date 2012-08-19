using System.ComponentModel.Composition;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class FolderResolver : BaseFolderResolver<Folder>
    {
        protected override Folder Resolve(ItemResolveEventArgs args)
        {
            if (args.IsFolder)
            {
                return new Folder();
            }

            return null;
        }
    }

    public abstract class BaseFolderResolver<TItemType> : BaseItemResolver<TItemType>
        where TItemType : Folder, new()
    {
        protected override void SetInitialItemValues(TItemType item, ItemResolveEventArgs args)
        {
            base.SetInitialItemValues(item, args);

            item.IsRoot = args.Parent == null;
        }
    }
}
