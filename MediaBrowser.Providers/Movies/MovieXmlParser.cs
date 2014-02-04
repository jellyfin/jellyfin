using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Xml;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class MovieXmlParser : BaseItemXmlParser<Video>
    {
        public MovieXmlParser(ILogger logger)
            : base(logger)
        {
        }

        public void FetchAsync(Video item, string metadataFile, CancellationToken cancellationToken)
        {
            Fetch(item, metadataFile, cancellationToken);
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
