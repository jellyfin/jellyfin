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
                        // Get ids from attributes
                        item.TrySetProviderId(MetadataProvider.Tmdb, reader.GetAttribute("TMDB"));
                        item.TrySetProviderId(MetadataProvider.Tvdb, reader.GetAttribute("TVDB"));
                        string? imdbId = reader.GetAttribute("IMDB");

                        // Read id from content
                        // Content can be arbitrary according to Kodi wiki, so only parse if we are sure it matches a provider-specific schema
                        var contentId = reader.ReadElementContentAsString();
                        if (string.IsNullOrEmpty(imdbId) && contentId.StartsWith("tt", StringComparison.Ordinal))
                        {
                            imdbId = contentId;
                        }

                        item.TrySetProviderId(MetadataProvider.Imdb, imdbId);

                        break;
                    }

                case "set":
                    {
                        var movie = item as Movie;

                        var tmdbcolid = reader.GetAttribute("tmdbcolid");
                        movie?.TrySetProviderId(MetadataProvider.TmdbCollection, tmdbcolid);

                        var val = reader.ReadInnerXml();

                        if (!string.IsNullOrWhiteSpace(val) && movie is not null)
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
                        if (reader.NodeType == XmlNodeType.Text && reader.Depth == 1)
                        {
                            movie.CollectionName = reader.Value;
                            break;
                        }
                        else if (reader.NodeType == XmlNodeType.Element)
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
