using System;
using System.Globalization;
using System.Xml;
using Emby.Naming.TV;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
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
        }

        /// <inheritdoc />
        protected override bool SupportsUrlAfterClosingXmlTag => true;

        /// <inheritdoc />
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Series> itemResult)
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

                case "airs_dayofweek":
                    item.AirDays = TVUtils.GetAirDays(reader.ReadElementContentAsString());
                    break;
                case "airs_time":
                    item.AirTime = reader.ReadNormalizedString();
                    break;
                case "status":
                    {
                        var status = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            if (TvParserHelpers.TryParseSeriesStatus(status, out var seriesStatus))
                            {
                                item.Status = seriesStatus;
                            }
                            else
                            {
                                Logger.LogInformation("Unrecognized series status: {Status}", status);
                            }
                        }

                        break;
                    }

                // Season names are processed by SeriesNfoSeasonParser
                case "namedseason":
                    reader.Skip();
                    break;
                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
