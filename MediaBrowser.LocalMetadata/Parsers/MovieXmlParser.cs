using System.Collections.Generic;
using System.Threading;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class MovieXmlParser : BaseItemXmlParser<Video>
    {
        private List<ChapterInfo> _chaptersFound;

        public MovieXmlParser(ILogger logger)
            : base(logger)
        {
        }

        public void Fetch(Video item, 
            List<ChapterInfo> chapters, 
            string metadataFile, 
            CancellationToken cancellationToken)
        {
            _chaptersFound = chapters;

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

                    _chaptersFound.AddRange(FetchChaptersFromXmlNode(item, reader.ReadSubtree()));
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
