using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.TV.Entities;
using MediaBrowser.TV.Metadata;

namespace MediaBrowser.TV.Providers
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

        public override Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.ContainsFile("series.xml"))
            {
                return Task.Run(() => { new SeriesXmlParser().Fetch(item as Series, Path.Combine(args.Path, "series.xml")); });
            }

            return Task.FromResult<object>(null);
        }
    }
}
