using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Server.Implementations.Library.Interfaces;
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
        /// <param name="genreManager">Instance of the <see cref="IGenreManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        public SeriesNfoParser(
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
        protected override bool SupportsUrlAfterClosingXmlTag => true;

        /// <inheritdoc />
        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<Series> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "id":
                    {
                        string? imdbId = reader.GetAttribute("IMDB");
                        string? tmdbId = reader.GetAttribute("TMDB");
                        string? tvdbId = reader.GetAttribute("TVDB");

                        if (string.IsNullOrWhiteSpace(tvdbId))
                        {
                            tvdbId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        }

                        if (!string.IsNullOrWhiteSpace(imdbId))
                        {
                            item.SetProviderId(MetadataProvider.Imdb, imdbId);
                        }

                        if (!string.IsNullOrWhiteSpace(tmdbId))
                        {
                            item.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                        }

                        if (!string.IsNullOrWhiteSpace(tvdbId))
                        {
                            item.SetProviderId(MetadataProvider.Tvdb, tvdbId);
                        }

                        break;
                    }

                case "airs_dayofweek":
                    item.AirDays = TVUtils.GetAirDays(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                    break;
                case "airs_time":
                    item.AirTime = reader.ReadNormalizedString();
                    break;
                case "status":
                    {
                        var status = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            if (Enum.TryParse(status, true, out SeriesStatus seriesStatus))
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

                case "namedseason":
                    {
                        var parsed = int.TryParse(reader.GetAttribute("number"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seasonNumber);
                        var name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(name) && parsed)
                        {
                            item.SeasonNames[seasonNumber] = name;
                        }

                        break;
                    }

                default:
                    await base.FetchDataFromXmlNode(reader, itemResult).ConfigureAwait(false);
                    break;
            }
        }
    }
}
