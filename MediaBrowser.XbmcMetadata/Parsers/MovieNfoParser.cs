using System;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    class MovieNfoParser : BaseNfoParser<Video>
    {
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

                        var val = reader.ReadInnerXml();

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            // TODO Handle this better later
                            if (val.IndexOf('<') == -1)
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
                        var movie = item as MusicVideo;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
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

        private void ParseSetXml(string xml, Movie movie)
        {
            //xml = xml.Substring(xml.IndexOf('<'));
            //xml = xml.Substring(0, xml.LastIndexOf('>'));

            using (var stringReader = new StringReader("<set>" + xml + "</set>"))
            {
                // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
                try
                {
                    var settings = XmlReaderSettingsFactory.Create(false);

                    settings.CheckCharacters = false;
                    settings.IgnoreProcessingInstructions = true;
                    settings.IgnoreComments = true;

                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(stringReader, settings))
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

        public MovieNfoParser(ILogger logger, IConfigurationManager config, IProviderManager providerManager, IFileSystem fileSystem, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(logger, config, providerManager, fileSystem, xmlReaderSettingsFactory)
        {
        }
    }
}
