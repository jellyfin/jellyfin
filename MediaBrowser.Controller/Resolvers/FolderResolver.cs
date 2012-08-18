using System.ComponentModel.Composition;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Xml;
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
        protected override void SetItemValues(TItemType item, ItemResolveEventArgs args)
        {
            base.SetItemValues(item, args);

            item.IsRoot = args.Parent == null;

            // Read data from folder.xml, if it exists
            PopulateFolderMetadata(item, args);
        }

        private void PopulateFolderMetadata(TItemType folder, ItemResolveEventArgs args)
        {
            var metadataFile = args.GetFileByName("folder.xml");

            if (metadataFile.HasValue)
            {
                new FolderXmlParser().Fetch(folder, metadataFile.Value.Key);
            }
        }
    }
}
