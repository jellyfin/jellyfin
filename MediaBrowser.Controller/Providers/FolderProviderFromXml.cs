using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides metadata for Folders and all subclasses by parsing folder.xml
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    public class FolderProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Folder;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public async override Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.ContainsFile("folder.xml"))
            {
                await Task.Run(() => Fetch(item, args)).ConfigureAwait(false);
            }
        }

        private void Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            new BaseItemXmlParser<Folder>().Fetch(item as Folder, Path.Combine(args.Path, "folder.xml"));
        }
    }
}
