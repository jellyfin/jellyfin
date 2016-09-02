using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Xml;

namespace MediaBrowser.LocalMetadata.Parsers
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class BaseVideoXmlParser<T> : BaseItemXmlParser<T>
        where T : Video
    {
        public BaseVideoXmlParser(ILogger logger, IProviderManager providerManager)
            : base(logger, providerManager)
        {
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="result">The result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "TmdbCollectionName":
                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as Movie;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.CollectionName = val;
                        }

                        break;
                    }

                default:
                    base.FetchDataFromXmlNode(reader, result);
                    break;
            }
        }
    }

    public class MovieXmlParser : BaseVideoXmlParser<Movie>
    {
        public MovieXmlParser(ILogger logger, IProviderManager providerManager) : base(logger, providerManager)
        {
        }
    }

    public class VideoXmlParser : BaseVideoXmlParser<Video>
    {
        public VideoXmlParser(ILogger logger, IProviderManager providerManager)
            : base(logger, providerManager)
        {
        }
    }
}
