using System;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    /// <summary>
    /// Nfo parser for movies.
    /// </summary>
    public class MovieNfoParser : BaseNfoParser<Video>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovieNfoParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">the configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public MovieNfoParser(ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(logger, config, providerManager)
        {
        }

        /// <inheritdoc />
        protected override bool SupportsUrlAfterClosingXmlTag => true;

        /// <inheritdoc />
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

                        var val = reader.ReadInnerXml();

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            // TODO Handle this better later
                            if (val.IndexOf('<', StringComparison.Ordinal) == -1)
                            {
                                movie.CollectionName = val;
                            }
                            else
                            {
                                try
                                {
                                    ParseSetXml(val, movie);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, "Error parsing set node");
                                }
                            }
                        }

                        break;
                    }

                case "artist":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val) && item is MusicVideo movie)
                        {
                            var list = movie.Artists.ToList();
                            list.Add(val);
                            movie.Artists = list.ToArray();
                        }

                        break;
                    }

                case "album":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val) && item is MusicVideo movie)
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

        private void ParseSetXml(string xml, Movie movie)
        {
            // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
            try
            {
                using (var stringReader = new StringReader("<set>" + xml + "</set>"))
                using (var reader = XmlReader.Create(stringReader, GetXmlReaderSettings()))
                {
                    reader.MoveToContent();
                    reader.Read();

                    // Loop through each element
                    while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "name":
                                    movie.CollectionName = reader.ReadElementContentAsString();
                                    break;
                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }
            }
            catch (XmlException)
            {
            }
        }
    }
}
