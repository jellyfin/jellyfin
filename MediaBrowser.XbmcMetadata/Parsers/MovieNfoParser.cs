using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    class MovieNfoParser : BaseNfoParser<Video>
    {
        public MovieNfoParser(ILogger logger, IConfigurationManager config)
            : base(logger, config)
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
                    {
                        string imdbId = reader.GetAttribute("IMDB");
                        string tmdbId = reader.GetAttribute("TMDB");

                        if (string.IsNullOrWhiteSpace(imdbId))
                        {
                            imdbId = reader.ReadElementContentAsString();
                        }
                        if (!string.IsNullOrWhiteSpace(imdbId))
                        {
                            item.SetProviderId(MetadataProviders.Imdb, imdbId);
                        }
                        if (!string.IsNullOrWhiteSpace(tmdbId))
                        {
                            item.SetProviderId(MetadataProviders.Tmdb, tmdbId);
                        }
                        break;
                    }
                case "set":
                    {
                        var movie = item as Movie;

                        var tmdbcolid = reader.GetAttribute("tmdbcolid");
                        if (!string.IsNullOrWhiteSpace(tmdbcolid) && movie != null)
                        {
                            movie.SetProviderId(MetadataProviders.TmdbCollection, tmdbcolid);
                        }

                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.CollectionName = val;
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
