using System.ComponentModel.Composition;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;
using MediaBrowser.Movies.Entities;

namespace MediaBrowser.Movies.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class MovieProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseItem item)
        {
            return item is Movie;
        }

        public override Task Fetch(BaseItem item, ItemResolveEventArgs args)
        {
            return Task.Run(() =>
            {
                var metadataFile = args.GetFileByName("movie.xml");

                if (metadataFile.HasValue)
                {
                    new BaseItemXmlParser<Movie>().Fetch(item as Movie, metadataFile.Value.Key);
                }
            });
        }
    }
}
