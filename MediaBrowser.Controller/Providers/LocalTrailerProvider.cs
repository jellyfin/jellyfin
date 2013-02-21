using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides local trailers by checking the trailers subfolder
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    public class LocalTrailerProvider : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is BaseItem;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public async override Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.ContainsFolder("trailers"))
            {
                var items = new List<Video>();

                foreach (WIN32_FIND_DATA file in FileData.GetFileSystemEntries(Path.Combine(args.Path, "trailers"), "*"))
                {
                    var video = await Kernel.Instance.ItemController.GetItem(file.Path, fileInfo: file).ConfigureAwait(false) as Video;

                    if (video != null)
                    {
                        items.Add(video);
                    }
                }

                (item as BaseItem).LocalTrailers = items;
            }
        }
    }
}
