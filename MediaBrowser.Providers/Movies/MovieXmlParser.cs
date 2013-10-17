using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class MovieXmlParser : BaseItemXmlParser<Video>
    {
        private readonly IItemRepository _itemRepo;

        private Task _chaptersTask = null;

        public MovieXmlParser(ILogger logger, IItemRepository itemRepo)
            : base(logger)
        {
            _itemRepo = itemRepo;
        }

        public async Task FetchAsync(Video item, string metadataFile, CancellationToken cancellationToken)
        {
            _chaptersTask = null;

            Fetch(item, metadataFile, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (_chaptersTask != null)
            {
                await _chaptersTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Video item)
        {
            switch (reader.Name)
            {
                case "TmdbCollectionName":

                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as Movie;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.TmdbCollectionName = val;
                        }
                        
                        break;
                    }

                case "Chapters":

                    //_chaptersTask = FetchChaptersFromXmlNode(item, reader.ReadSubtree(), _itemRepo, CancellationToken.None);
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
