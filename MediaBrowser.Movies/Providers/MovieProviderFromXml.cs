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
        public override bool Supports(BaseEntity item)
        {
            return item is Movie;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public async override Task Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            var metadataFile = args.GetFileSystemEntryByName("movie.xml", false);

            if (metadataFile.HasValue)
            {
                await Task.Run(() => { new BaseItemXmlParser<Movie>().Fetch(item as Movie, metadataFile.Value.Key); }).ConfigureAwait(false);
            }
        }
    }
}
