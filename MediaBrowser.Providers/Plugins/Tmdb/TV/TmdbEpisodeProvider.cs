using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
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
    /// TV episode provider powered by TheMovieDb.
    /// </summary>
    public class TmdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbEpisodeProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TmdbClientManager"/>.</param>
        public TmdbEpisodeProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public int Order => 1;

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            // The search query must either provide an episode number or date
            if (!searchInfo.IndexNumber.HasValue)
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            var metadataResult = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

            if (!metadataResult.HasMetadata)
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            var item = metadataResult.Item;

            return new[]
            {
                new RemoteSearchResult
                {
                    IndexNumber = item.IndexNumber,
                    Name = item.Name,
                    ParentIndexNumber = item.ParentIndexNumber,
                    PremiereDate = item.PremiereDate,
                    ProductionYear = item.ProductionYear,
                    ProviderIds = item.ProviderIds,
                    SearchProviderName = Name,
                    IndexNumberEnd = item.IndexNumberEnd
                }
            };
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var metadataResult = new MetadataResult<Episode>();

            // Allowing this will dramatically increase scan times
            if (info.IsMissingEpisode)
            {
                return metadataResult;
            }

            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out string? tmdbId);

            var seriesTmdbId = Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture);
            if (seriesTmdbId <= 0)
            {
                return metadataResult;
            }

            var seasonNumber = info.ParentIndexNumber ?? 1;
            var episodeNumber = info.IndexNumber;

            if (!episodeNumber.HasValue)
            {
                return metadataResult;
            }

            TvEpisode? episodeResult = null;
            if (info.IndexNumberEnd.HasValue)
            {
                var startindex = episodeNumber;
                var endindex = info.IndexNumberEnd;
                List<TvEpisode>? result = null;
                for (int? episode = startindex; episode <= endindex; episode++)
                {
                    var episodeInfo = await _tmdbClientManager.GetEpisodeAsync(seriesTmdbId, seasonNumber, episode.Value, info.SeriesDisplayOrder, info.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(info.MetadataLanguage), cancellationToken).ConfigureAwait(false);
                    if (episodeInfo is not null)
                    {
                        (result ??= new List<TvEpisode>()).Add(episodeInfo);
                    }
                }

                if (result is not null)
                {
                    // Forces a deep copy of the first TvEpisode, so we don't modify the original because it's cached
                    episodeResult = new TvEpisode()
                    {
                        Name = result[0].Name,
                        Overview = result[0].Overview,
                        AirDate = result[0].AirDate,
                        VoteAverage = result[0].VoteAverage,
                        ExternalIds = result[0].ExternalIds,
                        Videos = result[0].Videos,
                        Credits = result[0].Credits
                    };

                    if (result.Count > 1)
                    {
                        var name = new StringBuilder(episodeResult.Name);
                        var overview = new StringBuilder(episodeResult.Overview);

                        for (int i = 1; i < result.Count; i++)
                        {
                            name.Append(" / ").Append(result[i].Name);
                            overview.Append(" / ").Append(result[i].Overview);
                        }

                        episodeResult.Name = name.ToString();
                        episodeResult.Overview = overview.ToString();
                    }
                }
                else
                {
                    return metadataResult;
                }
            }
            else
            {
                episodeResult = await _tmdbClientManager
                    .GetEpisodeAsync(seriesTmdbId, seasonNumber, episodeNumber.Value, info.SeriesDisplayOrder, info.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(info.MetadataLanguage), cancellationToken)
                    .ConfigureAwait(false);
            }

            if (episodeResult is null)
            {
                return metadataResult;
            }

            metadataResult.HasMetadata = true;
            metadataResult.QueriedById = true;

            if (!string.IsNullOrEmpty(episodeResult.Overview))
            {
                // if overview is non-empty, we can assume that localized data was returned
                metadataResult.ResultLanguage = info.MetadataLanguage;
            }

            var item = new Episode
            {
                IndexNumber = info.IndexNumber,
                ParentIndexNumber = info.ParentIndexNumber,
                IndexNumberEnd = info.IndexNumberEnd,
                Name = episodeResult.Name,
                PremiereDate = episodeResult.AirDate,
                ProductionYear = episodeResult.AirDate?.Year,
                Overview = episodeResult.Overview,
                CommunityRating = Convert.ToSingle(episodeResult.VoteAverage)
            };

            var externalIds = episodeResult.ExternalIds;
            item.TrySetProviderId(MetadataProvider.Tvdb, externalIds?.TvdbId);
            item.TrySetProviderId(MetadataProvider.Imdb, externalIds?.ImdbId);
            item.TrySetProviderId(MetadataProvider.TvRage, externalIds?.TvrageId);

            if (episodeResult.Videos?.Results is not null)
            {
                foreach (var video in episodeResult.Videos.Results)
                {
                    if (TmdbUtils.IsTrailerType(video))
                    {
                        item.AddTrailerUrl("https://www.youtube.com/watch?v=" + video.Key);
                    }
                }
            }

            var credits = episodeResult.Credits;

            if (credits?.Cast is not null)
            {
                foreach (var actor in credits.Cast.OrderBy(a => a.Order).Take(Plugin.Instance.Configuration.MaxCastMembers))
                {
                    metadataResult.AddPerson(new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character,
                        Type = PersonKind.Actor,
                        SortOrder = actor.Order
                    });
                }
            }

            if (credits?.GuestStars is not null)
            {
                foreach (var guest in credits.GuestStars.OrderBy(a => a.Order).Take(Plugin.Instance.Configuration.MaxCastMembers))
                {
                    metadataResult.AddPerson(new PersonInfo
                    {
                        Name = guest.Name.Trim(),
                        Role = guest.Character,
                        Type = PersonKind.GuestStar,
                        SortOrder = guest.Order
                    });
                }
            }

            // and the rest from crew
            if (credits?.Crew is not null)
            {
                foreach (var person in credits.Crew)
                {
                    // Normalize this
                    var type = TmdbUtils.MapCrewToPersonType(person);

                    if (!TmdbUtils.WantedCrewKinds.Contains(type)
                        && !TmdbUtils.WantedCrewTypes.Contains(person.Job ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    metadataResult.AddPerson(new PersonInfo
                    {
                        Name = person.Name.Trim(),
                        Role = person.Job,
                        Type = type
                    });
                }
            }

            metadataResult.Item = item;

            return metadataResult;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
