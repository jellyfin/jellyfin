using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TvDbSharper;
using TvDbSharper.Dto;

namespace MediaBrowser.Providers.Plugins.TheTvdb
{

    /// <summary>
    /// Class RemoteEpisodeProvider
    /// </summary>
    public class TvdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly TvdbClientManager _tvdbClientManager;

        public TvdbEpisodeProvider(IHttpClient httpClient, ILogger<TvdbEpisodeProvider> logger, TvdbClientManager tvdbClientManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tvdbClientManager = tvdbClientManager;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

            // Either an episode number or date must be provided; and the dictionary of provider ids must be valid
            if ((searchInfo.IndexNumber == null && searchInfo.PremiereDate == null)
                || !TvdbSeriesProvider.IsValidSeries(searchInfo.SeriesProviderIds))
            {
                return list;
            }

            var metadataResult = await GetEpisode(searchInfo, cancellationToken).ConfigureAwait(false);

            if (!metadataResult.HasMetadata)
            {
                return list;
            }

            var item = metadataResult.Item;

            list.Add(new RemoteSearchResult
            {
                IndexNumber = item.IndexNumber,
                Name = item.Name,
                ParentIndexNumber = item.ParentIndexNumber,
                PremiereDate = item.PremiereDate,
                ProductionYear = item.ProductionYear,
                ProviderIds = item.ProviderIds,
                SearchProviderName = Name,
                IndexNumberEnd = item.IndexNumberEnd
            });

            return list;
        }

        public string Name => "TheTVDB";

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>
            {
                QueriedById = true
            };

            if (TvdbSeriesProvider.IsValidSeries(searchInfo.SeriesProviderIds) &&
                (searchInfo.IndexNumber.HasValue || searchInfo.PremiereDate.HasValue))
            {
                result = await GetEpisode(searchInfo, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogDebug("No series identity found for {EpisodeName}", searchInfo.Name);
            }

            return result;
        }

        private async Task<MetadataResult<Episode>> GetEpisode(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>
            {
                QueriedById = true
            };

            string seriesTvdbId = searchInfo.GetProviderId(MetadataProviders.Tvdb);
            string episodeTvdbId = null;
            try
            {
                episodeTvdbId = await _tvdbClientManager
                    .GetEpisodeTvdbId(searchInfo, searchInfo.MetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);
                if (string.IsNullOrEmpty(episodeTvdbId))
                {
                    _logger.LogError("Episode {SeasonNumber}x{EpisodeNumber} not found for series {SeriesTvdbId}",
                        searchInfo.ParentIndexNumber, searchInfo.IndexNumber, seriesTvdbId);
                    return result;
                }

                var episodeResult = await _tvdbClientManager.GetEpisodesAsync(
                    Convert.ToInt32(episodeTvdbId), searchInfo.MetadataLanguage,
                    cancellationToken).ConfigureAwait(false);

                result = MapEpisodeToResult(searchInfo, episodeResult.Data);
            }
            catch (TvDbServerException e)
            {
                _logger.LogError(e, "Failed to retrieve episode with id {EpisodeTvDbId}, series id {SeriesTvdbId}", episodeTvdbId, seriesTvdbId);
            }

            return result;
        }

        private static MetadataResult<Episode> MapEpisodeToResult(EpisodeInfo id, EpisodeRecord episode)
        {
            var result = new MetadataResult<Episode>
            {
                HasMetadata = true,
                Item = new Episode
                {
                    IndexNumber = id.IndexNumber,
                    ParentIndexNumber = id.ParentIndexNumber,
                    IndexNumberEnd = id.IndexNumberEnd,
                    AirsBeforeEpisodeNumber = episode.AirsBeforeEpisode,
                    AirsAfterSeasonNumber = episode.AirsAfterSeason,
                    AirsBeforeSeasonNumber = episode.AirsBeforeSeason,
                    Name = episode.EpisodeName,
                    Overview = episode.Overview,
                    CommunityRating = (float?)episode.SiteRating,

                }
            };
            result.ResetPeople();

            var item = result.Item;
            item.SetProviderId(MetadataProviders.Tvdb, episode.Id.ToString());
            item.SetProviderId(MetadataProviders.Imdb, episode.ImdbId);

            if (string.Equals(id.SeriesDisplayOrder, "dvd", StringComparison.OrdinalIgnoreCase))
            {
                item.IndexNumber = Convert.ToInt32(episode.DvdEpisodeNumber ?? episode.AiredEpisodeNumber);
                item.ParentIndexNumber = episode.DvdSeason ?? episode.AiredSeason;
            }
            else if (episode.AiredEpisodeNumber.HasValue)
            {
                item.IndexNumber = episode.AiredEpisodeNumber;
            }
            else if (episode.AiredSeason.HasValue)
            {
                item.ParentIndexNumber = episode.AiredSeason;
            }

            if (DateTime.TryParse(episode.FirstAired, out var date))
            {
                // dates from tvdb are UTC but without offset or Z
                item.PremiereDate = date;
                item.ProductionYear = date.Year;
            }

            foreach (var director in episode.Directors)
            {
                result.AddPerson(new PersonInfo
                {
                    Name = director,
                    Type = PersonType.Director
                });
            }

            // GuestStars is a weird list of names and roles
            // Example:
            // 1: Some Actor (Role1
            // 2: Role2
            // 3: Role3)
            // 4: Another Actor (Role1
            // ...
            for (var i = 0; i < episode.GuestStars.Length; ++i)
            {
                var currentActor = episode.GuestStars[i];
                var roleStartIndex = currentActor.IndexOf('(');

                if (roleStartIndex == -1)
                {
                    result.AddPerson(new PersonInfo
                    {
                        Type = PersonType.GuestStar,
                        Name = currentActor,
                        Role = string.Empty
                    });
                    continue;
                }

                var roles = new List<string> { currentActor.Substring(roleStartIndex + 1) };

                // Fetch all roles
                for (var j = i + 1; j < episode.GuestStars.Length; ++j)
                {
                    var currentRole = episode.GuestStars[j];
                    var roleEndIndex = currentRole.IndexOf(')');

                    if (roleEndIndex == -1)
                    {
                        roles.Add(currentRole);
                        continue;
                    }

                    roles.Add(currentRole.TrimEnd(')'));
                    // Update the outer index (keep in mind it adds 1 after the iteration)
                    i = j;
                    break;
                }

                result.AddPerson(new PersonInfo
                {
                    Type = PersonType.GuestStar,
                    Name = currentActor.Substring(0, roleStartIndex).Trim(),
                    Role = string.Join(", ", roles)
                });
            }

            foreach (var writer in episode.Writers)
            {
                result.AddPerson(new PersonInfo
                {
                    Name = writer,
                    Type = PersonType.Writer
                });
            }

            result.ResultLanguage = episode.Language.EpisodeName;
            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        public int Order => 0;
    }
}
