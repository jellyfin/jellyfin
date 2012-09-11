using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.TV
{
    [Export(typeof(BaseMetadataProvider))]
    public class SeriesProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Series;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public override async Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            await Task.Run(() => Fetch(item, args)).ConfigureAwait(false);
        }

        private void Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.ContainsFile("series.xml"))
            {
                new SeriesXmlParser().Fetch(item as Series, Path.Combine(args.Path, "series.xml"));
            }
        }
    }
}
