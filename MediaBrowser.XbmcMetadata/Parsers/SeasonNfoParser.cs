using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Server.Implementations.Library.Interfaces;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    /// <summary>
    /// Nfo parser for seasons.
    /// </summary>
    public class SeasonNfoParser : BaseNfoParser<Season>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonNfoParser"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="genreManager">Instance of the <see cref="IGenreManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="DirectoryService"/> interface.</param>
        public SeasonNfoParser(
            ILogger logger,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            IGenreManager genreManager,
            IDirectoryService directoryService)
            : base(logger, config, providerManager, userManager, userDataManager, genreManager, directoryService)
        {
        }

        /// <inheritdoc />
        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<Season> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "seasonnumber":
                    if (reader.TryReadInt(out var seasonNumber))
                    {
                        item.IndexNumber = seasonNumber;
                    }

                    break;
                case "seasonname":
                    item.Name = reader.ReadNormalizedString();
                    break;
                default:
                    await base.FetchDataFromXmlNode(reader, itemResult).ConfigureAwait(false);
                    break;
            }
        }
    }
}
