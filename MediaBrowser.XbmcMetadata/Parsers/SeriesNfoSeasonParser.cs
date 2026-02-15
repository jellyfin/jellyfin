using System.Globalization;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    /// <summary>
    /// NFO parser for seasons based on series NFO.
    /// </summary>
    public class SeriesNfoSeasonParser : BaseNfoParser<Season>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesNfoSeasonParser"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        public SeriesNfoSeasonParser(
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
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Season> itemResult)
        {
            var item = itemResult.Item;

            if (reader.Name == "namedseason")
            {
                var parsed = int.TryParse(reader.GetAttribute("number"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seasonNumber);
                var name = reader.ReadElementContentAsString();

                if (parsed && !string.IsNullOrWhiteSpace(name) && item.IndexNumber.HasValue && seasonNumber == item.IndexNumber.Value)
                {
                    item.Name = name;
                }
            }
            else
            {
                reader.Skip();
            }
        }
    }
}
