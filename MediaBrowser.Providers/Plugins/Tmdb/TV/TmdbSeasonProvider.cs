using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using TMDbLib.Objects.TvShows;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    /// <summary>
    /// TV season provider powered by TheMovieDb.
    /// </summary>
    public class TmdbSeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbSeasonProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TmdbClientManager"/>.</param>
        public TmdbSeasonProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>
            {
                ResultLanguage = info.MetadataLanguage
            };
            var config = Plugin.Instance.Configuration;

            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out string? seriesTmdbId);

            var seasonNumber = info.IndexNumber;

            if (!seasonNumber.HasValue || !TmdbUtils.TryParseTmdbId(seriesTmdbId, out var seriesId))
            {
                return result;
            }

            var seriesId = Convert.ToInt32(seriesTmdbId, CultureInfo.InvariantCulture);

            // When the series is ordered by an episode group, the season number counts groups rather than TMDb
            // seasons, so the group has to be resolved instead of fetching a season that holds unrelated episodes.
            var groupCollection = await _tmdbClientManager
                .GetSeriesGroupAsync(seriesId, info.SeriesDisplayOrder, info.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(info.MetadataLanguage, info.MetadataCountryCode), info.MetadataCountryCode, cancellationToken)
                .ConfigureAwait(false);

            var group = groupCollection?.Groups?.Find(g => g.Order == seasonNumber.Value);
            if (group is not null)
            {
                return MapGroupToSeason(group, seasonNumber.Value, config.ImportSeasonName);
            }

            var seasonResult = await _tmdbClientManager
                .GetSeasonAsync(seriesId, seasonNumber.Value, info.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(info.MetadataLanguage, info.MetadataCountryCode), info.MetadataCountryCode, cancellationToken)
                .ConfigureAwait(false);

            if (seasonResult is null)
            {
                return result;
            }

            result.HasMetadata = true;
            result.Item = new Season
            {
                IndexNumber = seasonNumber,
                Overview = seasonResult.Overview,
                PremiereDate = seasonResult.AirDate,
                ProductionYear = seasonResult.AirDate?.Year
            };

            if (config.ImportSeasonName)
            {
                result.Item.Name = seasonResult.Name;
            }

            result.Item.TrySetProviderId(MetadataProvider.Tmdb, seasonResult.Id?.ToString(CultureInfo.InvariantCulture));
            result.Item.TrySetProviderId(MetadataProvider.Tvdb, seasonResult.ExternalIds?.TvdbId);

            var credits = seasonResult.Credits;
            if (credits?.Cast is not null)
            {
                var castQuery = config.HideMissingCastMembers
                    ? credits.Cast.Where(a => !string.IsNullOrEmpty(a.ProfilePath)).OrderBy(a => a.Order)
                    : credits.Cast.OrderBy(a => a.Order);

                foreach (var actor in castQuery.Take(config.MaxCastMembers))
                {
                    if (string.IsNullOrWhiteSpace(actor.Name))
                    {
                        continue;
                    }

                    var personInfo = new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character?.Trim() ?? string.Empty,
                        Type = PersonKind.Actor,
                        SortOrder = actor.Order,
                        ImageUrl = _tmdbClientManager.GetProfileUrl(actor.ProfilePath)
                    };

                    if (actor.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProvider.Tmdb, actor.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    result.AddPerson(personInfo);
                }
            }

            if (credits?.Crew is not null)
            {
                var crewQuery = credits.Crew
                    .Select(crewMember => new
                    {
                        CrewMember = crewMember,
                        PersonType = TmdbUtils.MapCrewToPersonType(crewMember)
                    })
                    .Where(entry => TmdbUtils.WantedCrewKinds.Contains(entry.PersonType));

                if (config.HideMissingCrewMembers)
                {
                    crewQuery = crewQuery.Where(entry => !string.IsNullOrEmpty(entry.CrewMember.ProfilePath));
                }

                foreach (var entry in crewQuery.Take(config.MaxCrewMembers))
                {
                    var crewMember = entry.CrewMember;

                    if (string.IsNullOrWhiteSpace(crewMember.Name))
                    {
                        continue;
                    }

                    var personInfo = new PersonInfo
                    {
                        Name = crewMember.Name.Trim(),
                        Role = crewMember.Job?.Trim() ?? string.Empty,
                        Type = entry.PersonType,
                        ImageUrl = _tmdbClientManager.GetProfileUrl(crewMember.ProfilePath)
                    };

                    if (crewMember.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProvider.Tmdb, crewMember.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    result.AddPerson(personInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// Maps an episode group to a season.
        /// </summary>
        /// <remarks>
        /// A group spans whatever TMDb seasons its episodes come from, so only the group's own name and the air date
        /// of its first episode describe it. TMDb has neither an overview, artwork nor credits for a group.
        /// </remarks>
        /// <param name="group">The episode group the season maps to.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="importSeasonName">Whether the season name should be imported.</param>
        /// <returns>The season metadata.</returns>
        internal static MetadataResult<Season> MapGroupToSeason(TvGroup group, int seasonNumber, bool importSeasonName)
        {
            var airDate = group.Episodes?
                .Where(e => e.AirDate.HasValue)
                .Min(e => e.AirDate);

            var season = new Season
            {
                IndexNumber = seasonNumber,
                PremiereDate = airDate,
                ProductionYear = airDate?.Year
            };

            if (importSeasonName)
            {
                season.Name = group.Name;
            }

            season.TrySetProviderId(TmdbUtils.EpisodeGroupProviderKey, group.Id);

            return new MetadataResult<Season>
            {
                HasMetadata = true,
                Item = season
            };
        }

        /// <inheritdoc />
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
