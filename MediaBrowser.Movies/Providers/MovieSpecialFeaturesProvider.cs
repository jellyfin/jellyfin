using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Movies.Entities;

namespace MediaBrowser.Movies.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class MovieSpecialFeaturesProvider : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Movie;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public async override Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.ContainsFolder("specials"))
            {
                List<Video> items = new List<Video>();

                foreach (WIN32_FIND_DATA file in FileData.GetFileSystemEntries(Path.Combine(args.Path, "specials"), "*"))
                {
                    Video video = await Kernel.Instance.ItemController.GetItem(file.Path, fileInfo: file).ConfigureAwait(false) as Video;

                    if (video != null)
                    {
                        items.Add(video);
                    }
                }

                (item as Movie).SpecialFeatures = items;
            }
        }
    }
}
