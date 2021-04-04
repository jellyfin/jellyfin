using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
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
                    NfoParserHelpers.SetMovieids(reader, item);
                    break;

                case "set":
                    NfoSubtreeParsers<Video>.ReadSetNode(reader, (Movie)item);
                    break;

                case "artist":
                    var artist = reader.ReadStringFromNfo();
                    if (!string.IsNullOrWhiteSpace(artist) && item is MusicVideo musicVideo)
                    {
                        musicVideo.Artists = new string[] { artist };
                    }

                    break;

                case "album":
                    var album = reader.ReadStringFromNfo();
                    if (!string.IsNullOrWhiteSpace(album) && item is MusicVideo)
                    {
                        item.Album = album;
                    }

                    break;

                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
