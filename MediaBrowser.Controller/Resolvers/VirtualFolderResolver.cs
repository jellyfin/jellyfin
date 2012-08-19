using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class VirtualFolderResolver : BaseFolderResolver<VirtualFolder>
    {
        protected override VirtualFolder Resolve(ItemResolveEventArgs args)
        {
            if (args.IsFolder && args.Parent != null && args.Parent.IsRoot)
            {
                return new VirtualFolder();
            }

            return null;
        }

        protected override void SetInitialItemValues(VirtualFolder item, ItemResolveEventArgs args)
        {
            // Set the name initially by stripping off the [CollectionType=...]
            // The name can always be overridden later by folder.xml
            string pathName = Path.GetFileNameWithoutExtension(args.Path);

            string srch = "[collectiontype=";
            int index = pathName.IndexOf(srch, System.StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                item.Name = pathName.Substring(0, index).Trim();

                item.CollectionType = pathName.Substring(index + srch.Length).TrimEnd(']');
            }

            base.SetInitialItemValues(item, args);
        }

    }
}
