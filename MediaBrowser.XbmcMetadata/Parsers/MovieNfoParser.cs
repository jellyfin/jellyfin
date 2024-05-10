using System;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
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
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="DirectoryService"/> interface.</param>
        public MovieNfoParser(
            ILogger logger,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            IDirectoryService directoryService)
            : base(logger, config, providerManager, userManager, userDataManager, directoryService)
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
                        // get ids from attributes
                        string? imdbId = reader.GetAttribute("IMDB");
                        string? tmdbId = reader.GetAttribute("TMDB");

                        // read id from content
                        var contentId = reader.ReadElementContentAsString();
                        if (contentId.Contains("tt", StringComparison.Ordinal) && string.IsNullOrEmpty(imdbId))
                        {
                            imdbId = contentId;
                        }
                        else if (string.IsNullOrEmpty(tmdbId))
                        {
                            tmdbId = contentId;
                        }

                        if (!string.IsNullOrWhiteSpace(imdbId))
                        {
                            item.SetProviderId(MetadataProvider.Imdb, imdbId);
                        }

                        if (!string.IsNullOrWhiteSpace(tmdbId))
                        {
                            item.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                        }

                        break;
                    }

                case "set":
                    {
                        var movie = item as Movie;

                        var tmdbcolid = reader.GetAttribute("tmdbcolid");
                        if (!string.IsNullOrWhiteSpace(tmdbcolid) && movie is not null)
                        {
                            movie.SetProviderId(MetadataProvider.TmdbCollection, tmdbcolid);
                        }

                        var val = reader.ReadInnerXml();

                        if (!string.IsNullOrWhiteSpace(val) && movie is not null)
                        {
                            // TODO Handle this better later
                            if (!val.Contains('<', StringComparison.Ordinal))
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
                    var artist = reader.ReadNormalizedString();
                    if (!string.IsNullOrEmpty(artist) && item is MusicVideo artistVideo)
                    {
                        artistVideo.Artists = [..artistVideo.Artists, artist];
                    }

                    break;
                case "album":
                    var album = reader.ReadNormalizedString();
                    if (!string.IsNullOrEmpty(album) && item is MusicVideo albumVideo)
                    {
                        albumVideo.Album = album;
                    }

                    break;
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
