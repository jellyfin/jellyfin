using System.ComponentModel.Composition;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class FolderProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseItem item)
        {
            return item is Folder;
        }

        public async override Task Fetch(BaseItem item, ItemResolveEventArgs args)
        {
            var metadataFile = args.GetFileByName("folder.xml");

            if (metadataFile.HasValue)
            {
                await new FolderXmlParser().Fetch(item as Folder, metadataFile.Value.Key);
            }
        }
    }
}
