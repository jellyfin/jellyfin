#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TvDbSharper;
using TvDbSharper.Dto;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace MediaBrowser.Providers.Plugins.TheTvdb
{
    public class TvdbSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        internal static TvdbSeriesProvider Current { get; private set; }

        private readonly IHttpClient _httpClient;
        private readonly ILogger<TvdbSeriesProvider> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly TvdbClientManager _tvdbClientManager;

        public TvdbSeriesProvider(IHttpClient httpClient, ILogger<TvdbSeriesProvider> logger, ILibraryManager libraryManager, ILocalizationManager localizationManager, TvdbClientManager tvdbClientManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _libraryManager = libraryManager;
            _localizationManager = localizationManager;
            Current = this;
            _tvdbClientManager = tvdbClientManager;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            if (IsValidSeries(searchInfo.ProviderIds))
            {
                var metadata = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

                if (metadata.HasMetadata)
                {
                    return new List<RemoteSearchResult>
                    {
                        new RemoteSearchResult
                        {
                            Name = metadata.Item.Name,
                            PremiereDate = metadata.Item.PremiereDate,
                            ProductionYear = metadata.Item.ProductionYear,
                            ProviderIds = metadata.Item.ProviderIds,
                            SearchProviderName = Name
                        }
                    };
                }
            }

            return await FindSeries(searchInfo.Name, searchInfo.Year, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo itemId, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>
            {
                QueriedById = true
            };

            if (!IsValidSeries(itemId.ProviderIds))
            {
                result.QueriedById = false;
                await Identify(itemId).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (IsValidSeries(itemId.ProviderIds))
            {
                result.Item = new Series();
                result.HasMetadata = true;

                await FetchSeriesData(result, itemId.MetadataLanguage, itemId.ProviderIds, cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }

        private async Task FetchSeriesData(MetadataResult<Series> result, string metadataLanguage, Dictionary<string, string> seriesProviderIds, CancellationToken cancellationToken)
        {
            var series = result.Item;

            if (seriesProviderIds.TryGetValue(MetadataProvider.Tvdb.ToString(), out var tvdbId) && !string.IsNullOrEmpty(tvdbId))
            {
                series.SetProviderId(MetadataProvider.Tvdb, tvdbId);
            }

            if (seriesProviderIds.TryGetValue(MetadataProvider.Imdb.ToString(), out var imdbId) && !string.IsNullOrEmpty(imdbId))
            {
                series.SetProviderId(MetadataProvider.Imdb, imdbId);
                tvdbId = await GetSeriesByRemoteId(imdbId, MetadataProvider.Imdb.ToString(), metadataLanguage,
                    cancellationToken).ConfigureAwait(false);
            }

            if (seriesProviderIds.TryGetValue(MetadataProvider.Zap2It.ToString(), out var zap2It) && !string.IsNullOrEmpty(zap2It))
            {
                series.SetProviderId(MetadataProvider.Zap2It, zap2It);
                tvdbId = await GetSeriesByRemoteId(zap2It, MetadataProvider.Zap2It.ToString(), metadataLanguage,
                    cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var seriesResult =
                    await _tvdbClientManager
                        .GetSeriesByIdAsync(Convert.ToInt32(tvdbId), metadataLanguage, cancellationToken)
                        .ConfigureAwait(false);
                MapSeriesToResult(result, seriesResult.Data, metadataLanguage);
            }
            catch (TvDbServerException e)
            {
                _logger.LogError(e, "Failed to retrieve series with id {TvdbId}", tvdbId);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            result.ResetPeople();

            try
            {
                var actorsResult = await _tvdbClientManager
                    .GetActorsAsync(Convert.ToInt32(tvdbId), metadataLanguage, cancellationToken).ConfigureAwait(false);
                MapActorsToResult(result, actorsResult.Data);
            }
            catch (TvDbServerException e)
            {
                _logger.LogError(e, "Failed to retrieve actors for series {TvdbId}", tvdbId);
            }
        }

        private async Task<string> GetSeriesByRemoteId(string id, string idType, string language, CancellationToken cancellationToken)
        {
            TvDbResponse<SeriesSearchResult[]> result = null;

            try
            {
                if (string.Equals(idType, MetadataProvider.Zap2It.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    result = await _tvdbClientManager.GetSeriesByZap2ItIdAsync(id, language, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    result = await _tvdbClientManager.GetSeriesByImdbIdAsync(id, language, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (TvDbServerException e)
            {
                _logger.LogError(e, "Failed to retrieve series with remote id {RemoteId}", id);
            }

            return result?.Data.First().Id.ToString();
        }

        /// <summary>
        /// Check whether a dictionary of provider IDs includes an entry for a valid TV metadata provider.
        /// </summary>
        /// <param name="seriesProviderIds">The dictionary to check.</param>
        /// <returns>True, if the dictionary contains a valid TV provider ID, otherwise false.</returns>
        internal static bool IsValidSeries(Dictionary<string, string> seriesProviderIds)
        {
            return seriesProviderIds.ContainsKey(MetadataProvider.Tvdb.ToString()) ||
                   seriesProviderIds.ContainsKey(MetadataProvider.Imdb.ToString()) ||
                   seriesProviderIds.ContainsKey(MetadataProvider.Zap2It.ToString());
        }

        /// <summary>
        /// Finds the series.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="year">The year.</param>
        /// <param name="language">The language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<IEnumerable<RemoteSearchResult>> FindSeries(string name, int? year, string language, CancellationToken cancellationToken)
        {
            var results = await FindSeriesInternal(name, language, cancellationToken).ConfigureAwait(false);

            if (results.Count == 0)
            {
                var parsedName = _libraryManager.ParseName(name);
                var nameWithoutYear = parsedName.Name;

                if (!string.IsNullOrWhiteSpace(nameWithoutYear) && !string.Equals(nameWithoutYear, name, StringComparison.OrdinalIgnoreCase))
                {
                    results = await FindSeriesInternal(nameWithoutYear, language, cancellationToken).ConfigureAwait(false);
                }
            }

            return results.Where(i =>
            {
                if (year.HasValue && i.ProductionYear.HasValue)
                {
                    // Allow one year tolerance
                    return Math.Abs(year.Value - i.ProductionYear.Value) <= 1;
                }

                return true;
            });
        }

        private async Task<List<RemoteSearchResult>> FindSeriesInternal(string name, string language, CancellationToken cancellationToken)
        {
            var comparableName = GetComparableName(name);
            var list = new List<Tuple<List<string>, RemoteSearchResult>>();
            TvDbResponse<SeriesSearchResult[]> result;
            try
            {
                result = await _tvdbClientManager.GetSeriesByNameAsync(comparableName, language, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TvDbServerException e)
            {
                _logger.LogError(e, "No series results found for {Name}", comparableName);
                return new List<RemoteSearchResult>();
            }

            foreach (var seriesSearchResult in result.Data)
            {
                var tvdbTitles = new List<string>
                {
                    GetComparableName(seriesSearchResult.SeriesName)
                };
                tvdbTitles.AddRange(seriesSearchResult.Aliases.Select(GetComparableName));

                DateTime.TryParse(seriesSearchResult.FirstAired, out var firstAired);
                var remoteSearchResult = new RemoteSearchResult
                {
                    Name = tvdbTitles.FirstOrDefault(),
                    ProductionYear = firstAired.Year,
                    SearchProviderName = Name
                };

                if (!string.IsNullOrEmpty(seriesSearchResult.Banner))
                {
                    // Results from their Search endpoints already include the /banners/ part in the url, because reasons...
                    remoteSearchResult.ImageUrl = TvdbUtils.TvdbImageBaseUrl + seriesSearchResult.Banner;
                }

                try
                {
                    var seriesSesult =
                        await _tvdbClientManager.GetSeriesByIdAsync(seriesSearchResult.Id, language, cancellationToken)
                            .ConfigureAwait(false);
                    remoteSearchResult.SetProviderId(MetadataProvider.Imdb, seriesSesult.Data.ImdbId);
                    remoteSearchResult.SetProviderId(MetadataProvider.Zap2It, seriesSesult.Data.Zap2itId);
                }
                catch (TvDbServerException e)
                {
                    _logger.LogError(e, "Unable to retrieve series with id {TvdbId}", seriesSearchResult.Id);
                }

                remoteSearchResult.SetProviderId(MetadataProvider.Tvdb, seriesSearchResult.Id.ToString());
                list.Add(new Tuple<List<string>, RemoteSearchResult>(tvdbTitles, remoteSearchResult));
            }

            return list
                .OrderBy(i => i.Item1.Contains(comparableName, StringComparer.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(i => list.IndexOf(i))
                .Select(i => i.Item2)
                .ToList();
        }

        /// <summary>
        /// Gets the name of the comparable.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        private string GetComparableName(string name)
        {
            name = name.ToLowerInvariant();
            name = name.Normalize(NormalizationForm.FormKD);
            name = name.Replace(", the", string.Empty).Replace("the ", " ").Replace(" the ", " ");
            name = name.Replace("&", " and " );
            name = Regex.Replace(name, @"[\p{Lm}\p{Mn}]", string.Empty); // Remove diacritics, etc
            name = Regex.Replace(name, @"[\W\p{Pc}]+", " "); // Replace sequences of non-word characters and _ with " "
            return name.Trim();
        }

        private void MapSeriesToResult(MetadataResult<Series> result, TvDbSharper.Dto.Series tvdbSeries, string metadataLanguage)
        {
            Series series = result.Item;
            series.SetProviderId(MetadataProvider.Tvdb, tvdbSeries.Id.ToString());
            series.Name = tvdbSeries.SeriesName;
            series.Overview = (tvdbSeries.Overview ?? string.Empty).Trim();
            result.ResultLanguage = metadataLanguage;
            series.AirDays = TVUtils.GetAirDays(tvdbSeries.AirsDayOfWeek);
            series.AirTime = tvdbSeries.AirsTime;
            series.CommunityRating = (float?)tvdbSeries.SiteRating;
            series.SetProviderId(MetadataProvider.Imdb, tvdbSeries.ImdbId);
            series.SetProviderId(MetadataProvider.Zap2It, tvdbSeries.Zap2itId);
            if (Enum.TryParse(tvdbSeries.Status, true, out SeriesStatus seriesStatus))
            {
                series.Status = seriesStatus;
            }

            if (DateTime.TryParse(tvdbSeries.FirstAired, out var date))
            {
                // dates from tvdb are UTC but without offset or Z
                series.PremiereDate = date;
                series.ProductionYear = date.Year;
            }

            if (!string.IsNullOrEmpty(tvdbSeries.Runtime) && double.TryParse(tvdbSeries.Runtime, out double runtime))
            {
                series.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
            }

            foreach (var genre in tvdbSeries.Genre)
            {
                series.AddGenre(genre);
            }

            if (!string.IsNullOrEmpty(tvdbSeries.Network))
            {
                series.AddStudio(tvdbSeries.Network);
            }

            if (result.Item.Status.HasValue && result.Item.Status.Value == SeriesStatus.Ended)
            {
                try
                {
                    var episodeSummary = _tvdbClientManager
                        .GetSeriesEpisodeSummaryAsync(tvdbSeries.Id, metadataLanguage, CancellationToken.None).Result.Data;
                    var maxSeasonNumber = episodeSummary.AiredSeasons.Select(s => Convert.ToInt32(s)).Max();
                    var episodeQuery = new EpisodeQuery
                    {
                        AiredSeason = maxSeasonNumber
                    };
                    var episodesPage =
                        _tvdbClientManager.GetEpisodesPageAsync(tvdbSeries.Id, episodeQuery, metadataLanguage, CancellationToken.None).Result.Data;
                    result.Item.EndDate = episodesPage.Select(e =>
                        {
                            DateTime.TryParse(e.FirstAired, out var firstAired);
                            return firstAired;
                        }).Max();
                }
                catch (TvDbServerException e)
                {
                    _logger.LogError(e, "Failed to find series end date for series {TvdbId}", tvdbSeries.Id);
                }
            }
        }

        private static void MapActorsToResult(MetadataResult<Series> result, IEnumerable<Actor> actors)
        {
            foreach (Actor actor in actors)
            {
                var personInfo = new PersonInfo
                {
                    Type = PersonType.Actor,
                    Name = (actor.Name ?? string.Empty).Trim(),
                    Role = actor.Role,
                    SortOrder = actor.SortOrder
                };

                if (!string.IsNullOrEmpty(actor.Image))
                {
                    personInfo.ImageUrl = TvdbUtils.BannerUrl + actor.Image;
                }

                if (!string.IsNullOrWhiteSpace(personInfo.Name))
                {
                    result.AddPerson(personInfo);
                }
            }
        }

        public string Name => "TheTVDB";

        public async Task Identify(SeriesInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.GetProviderId(MetadataProvider.Tvdb)))
            {
                return;
            }

            var srch = await FindSeries(info.Name, info.Year, info.MetadataLanguage, CancellationToken.None)
                .ConfigureAwait(false);

            var entry = srch.FirstOrDefault();

            if (entry != null)
            {
                var id = entry.GetProviderId(MetadataProvider.Tvdb);
                info.SetProviderId(MetadataProvider.Tvdb, id);
            }
        }

        public int Order => 0;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                BufferContent = false
            });
        }
    }
}
