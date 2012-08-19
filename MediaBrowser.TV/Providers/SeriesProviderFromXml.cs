using System.ComponentModel.Composition;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class SeriesProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseItem item)
        {
            return item is Series;
        }

        public override Task Fetch(BaseItem item, ItemResolveEventArgs args)
        {
            return Task.Run(() =>
            {
                var metadataFile = args.GetFileByName("series.xml");

                if (metadataFile.HasValue)
                {
                    new BaseItemXmlParser<Series>().Fetch(item as Series, metadataFile.Value.Key);
                }
            });
        }
    }
}
