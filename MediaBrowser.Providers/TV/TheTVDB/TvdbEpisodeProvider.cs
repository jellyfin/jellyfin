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
using TvDbSharper.Dto;

namespace MediaBrowser.Providers.TV
{

    /// <summary>
    /// Class RemoteEpisodeProvider
    /// </summary>
    class TvdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        internal static TvdbEpisodeProvider Current;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly TvDbClientManager _tvDbClientManager;

        public TvdbEpisodeProvider(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            Current = this;
            _tvDbClientManager = TvDbClientManager.Instance;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

            // The search query must either provide an episode number or date
            // TODO premieredate functionality is ded
            if (!searchInfo.IndexNumber.HasValue || !searchInfo.PremiereDate.HasValue)
            {
                return list;
            }

            if (TvdbSeriesProvider.IsValidSeries(searchInfo.SeriesProviderIds))
            {
                var episodeResult =
                    await _tvDbClientManager.TvDbClient.Episodes.GetAsync((int)searchInfo.IndexNumber, cancellationToken);
                var metadataResult = MapEpisodeToResult(searchInfo, episodeResult.Data);

                if (metadataResult.HasMetadata)
                {
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
                }
            }

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
                var episodeResult = await _tvDbClientManager.TvDbClient.Episodes.GetAsync(Convert.ToInt32(searchInfo.GetProviderId(MetadataProviders.Tvdb)),
                    cancellationToken);

                result = MapEpisodeToResult(searchInfo, episodeResult.Data);
            }
            else
            {
                _logger.LogDebug("No series identity found for {0}", searchInfo.Name);
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
                date = date.ToUniversalTime();

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
            foreach (var person in episode.GuestStars)
            {
                var index = person.IndexOf('(');
                string role = null;
                var name = person;

                if (index != -1)
                {
                    role = person.Substring(index + 1).Trim().TrimEnd(')');

                    name = person.Substring(0, index).Trim();
                }

                result.AddPerson(new PersonInfo
                {
                    Type = PersonType.GuestStar,
                    Name = name,
                    Role = role
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
            // TODO result.ResultLanguage = episode.

            // TODO wtf is additional part info?
//            foreach (var node in xmlNodes.Skip(1))
//            {
//                FetchAdditionalPartInfo(result, node, cancellationToken);
//            }

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
