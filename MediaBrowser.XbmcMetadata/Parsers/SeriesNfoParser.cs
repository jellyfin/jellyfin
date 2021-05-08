using System;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    /// <summary>
    /// Nfo parser for series.
    /// </summary>
    public class SeriesNfoParser : BaseNfoParser<Series>
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesNfoParser"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        public SeriesNfoParser(
            ILogger logger,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            IDirectoryService directoryService)
            : base(logger, config, providerManager, userManager, userDataManager, directoryService)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        protected override bool SupportsUrlAfterClosingXmlTag => true;

        /// <inheritdoc />
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Series> itemResult)
        {
            var item = itemResult.Item;

            var parserHelpers = new NfoParserHelpers(_logger);

            switch (reader.Name)
            {
                case "id":
                    parserHelpers.SetSeriesIds(reader, item);
                    break;

                case "airs_dayofweek":
                    item.AirDays = TVUtils.GetAirDays(reader.ReadElementContentAsString());
                    break;

                case "airs_time":
                    item.AirTime = parserHelpers.ReadStringFromNfo(reader) ?? item.AirTime;
                    break;

                case "status":
                    var status = parserHelpers.ReadStringFromNfo(reader);
                    if (Enum.TryParse(status, true, out SeriesStatus parsedStatus))
                    {
                        item.Status = parsedStatus;
                    }

                    break;

                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
