using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Xml;

namespace MediaBrowser.Controller.Resolvers
{
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

    public abstract class BaseFolderResolver<T> : BaseItemResolver<T>
        where T : Folder, new ()
    {
        protected override void SetItemValues(T item, ItemResolveEventArgs args)
        {
            base.SetItemValues(item, args);

            item.IsRoot = args.Parent == null;

            // Read data from folder.xml, if it exists
            PopulateFolderMetadata(item, args);
        }

        private void PopulateFolderMetadata(Folder folder, ItemResolveEventArgs args)
        {
            var metadataFile = args.GetFileByName("folder.xml");

            if (metadataFile.HasValue)
            {
                new FolderXmlParser().Fetch(folder, metadataFile.Value.Key);
            }
        }
    }
}
