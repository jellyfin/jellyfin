using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    class MovieNfoParser : BaseNfoParser<Video>
    {
        public MovieNfoParser(ILogger logger, IConfigurationManager config) : base(logger, config)
        {
        }

        protected override bool SupportsUrlAfterClosingXmlTag
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="itemResult">The item result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Video> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "id":
                    var id = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.SetProviderId(MetadataProviders.Imdb, id);
                    }
                    break;

                case "set":
                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as Movie;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.TmdbCollectionName = val;
                        }

                        break;
                    }

                case "artist":
                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as MusicVideo;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.Artists.Add(val);
                        }

                        break;
                    }

                case "album":
                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as MusicVideo;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.Album = val;
                        }

                        break;
                    }

                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
