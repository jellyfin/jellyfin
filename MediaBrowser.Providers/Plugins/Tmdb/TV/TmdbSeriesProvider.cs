using System;
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
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    /// <summary>
    /// TV series provider powered by TheMovieDb.
    /// </summary>
    public class TmdbSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILibraryManager _libraryManager;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbSeriesProvider"/> class.
        /// </summary>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TmdbClientManager"/>.</param>
        public TmdbSeriesProvider(
            ILibraryManager libraryManager,
            IHttpClientFactory httpClientFactory,
            TmdbClientManager tmdbClientManager)
        {
            _libraryManager = libraryManager;
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public int Order => 1;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            if (searchInfo.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbId))
            {
                var series = await _tmdbClientManager
                    .GetSeriesAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), searchInfo.MetadataLanguage, searchInfo.MetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);

                if (series is not null)
                {
                    var remoteResult = MapTvShowToRemoteSearchResult(series);

                    return new[] { remoteResult };
                }
            }

            if (searchInfo.TryGetProviderId(MetadataProvider.Imdb, out var imdbId))
            {
                var findResult = await _tmdbClientManager
                    .FindByExternalIdAsync(imdbId, FindExternalSource.Imdb, searchInfo.MetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);

                var tvResults = findResult?.TvResults;
                if (tvResults is not null)
                {
                    var imdbIdResults = new RemoteSearchResult[tvResults.Count];
                    for (var i = 0; i < tvResults.Count; i++)
                    {
                        var remoteResult = MapSearchTvToRemoteSearchResult(tvResults[i]);
                        remoteResult.SetProviderId(MetadataProvider.Imdb, imdbId);
                        imdbIdResults[i] = remoteResult;
                    }

                    return imdbIdResults;
                }
            }

            if (searchInfo.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId))
            {
                var findResult = await _tmdbClientManager
                    .FindByExternalIdAsync(tvdbId, FindExternalSource.TvDb, searchInfo.MetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);

                var tvResults = findResult?.TvResults;
                if (tvResults is not null)
                {
                    var tvIdResults = new RemoteSearchResult[tvResults.Count];
                    for (var i = 0; i < tvResults.Count; i++)
                    {
                        var remoteResult = MapSearchTvToRemoteSearchResult(tvResults[i]);
                        remoteResult.SetProviderId(MetadataProvider.Tvdb, tvdbId);
                        tvIdResults[i] = remoteResult;
                    }

                    return tvIdResults;
                }
            }

            var tvSearchResults = await _tmdbClientManager.SearchSeriesAsync(searchInfo.Name, searchInfo.MetadataLanguage, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var remoteResults = new RemoteSearchResult[tvSearchResults.Count];
            for (var i = 0; i < tvSearchResults.Count; i++)
            {
                remoteResults[i] = MapSearchTvToRemoteSearchResult(tvSearchResults[i]);
            }

            return remoteResults;
        }

        private RemoteSearchResult MapTvShowToRemoteSearchResult(TvShow series)
        {
            var remoteResult = new RemoteSearchResult
            {
                Name = series.Name ?? series.OriginalName,
                SearchProviderName = Name,
                ImageUrl = _tmdbClientManager.GetPosterUrl(series.PosterPath),
                Overview = series.Overview
            };

            remoteResult.SetProviderId(MetadataProvider.Tmdb, series.Id.ToString(CultureInfo.InvariantCulture));
            if (series.ExternalIds is not null)
            {
                remoteResult.TrySetProviderId(MetadataProvider.Imdb, series.ExternalIds.ImdbId);

                remoteResult.TrySetProviderId(MetadataProvider.Tvdb, series.ExternalIds.TvdbId);
            }

            remoteResult.PremiereDate = series.FirstAirDate?.ToUniversalTime();

            return remoteResult;
        }

        private RemoteSearchResult MapSearchTvToRemoteSearchResult(SearchTv series)
        {
            var remoteResult = new RemoteSearchResult
            {
                Name = series.Name ?? series.OriginalName,
                SearchProviderName = Name,
                ImageUrl = _tmdbClientManager.GetPosterUrl(series.PosterPath),
                Overview = series.Overview
            };

            remoteResult.SetProviderId(MetadataProvider.Tmdb, series.Id.ToString(CultureInfo.InvariantCulture));
            remoteResult.PremiereDate = series.FirstAirDate?.ToUniversalTime();

            return remoteResult;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>
            {
                QueriedById = true
            };

            var tmdbId = info.GetProviderId(MetadataProvider.Tmdb);

            if (string.IsNullOrEmpty(tmdbId) && info.TryGetProviderId(MetadataProvider.Imdb, out var imdbId))
            {
                var searchResult = await _tmdbClientManager.FindByExternalIdAsync(imdbId, FindExternalSource.Imdb, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                if (searchResult?.TvResults.Count > 0)
                {
                    tmdbId = searchResult.TvResults[0].Id.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrEmpty(tmdbId) && info.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId))
            {
                var searchResult = await _tmdbClientManager.FindByExternalIdAsync(tvdbId, FindExternalSource.TvDb, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                if (searchResult?.TvResults.Count > 0)
                {
                    tmdbId = searchResult.TvResults[0].Id.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrEmpty(tmdbId))
            {
                result.QueriedById = false;
                // ParseName is required here.
                // Caller provides the filename with extension stripped and NOT the parsed filename
                var parsedName = _libraryManager.ParseName(info.Name);
                var cleanedName = TmdbUtils.CleanName(parsedName.Name);
                var searchResults = await _tmdbClientManager.SearchSeriesAsync(cleanedName, info.MetadataLanguage, info.Year ?? parsedName.Year ?? 0, cancellationToken).ConfigureAwait(false);

                if (searchResults.Count > 0)
                {
                    tmdbId = searchResults[0].Id.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (!int.TryParse(tmdbId, CultureInfo.InvariantCulture, out int tmdbIdInt))
            {
                return result;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var tvShow = await _tmdbClientManager
                .GetSeriesAsync(tmdbIdInt, info.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(info.MetadataLanguage), cancellationToken)
                .ConfigureAwait(false);

            if (tvShow is null)
            {
                return result;
            }

            result = new MetadataResult<Series>
            {
                Item = MapTvShowToSeries(tvShow, info.MetadataCountryCode),
                ResultLanguage = info.MetadataLanguage ?? tvShow.OriginalLanguage
            };

            foreach (var person in GetPersons(tvShow))
            {
                result.AddPerson(person);
            }

            result.HasMetadata = result.Item is not null;

            return result;
        }

        private static Series MapTvShowToSeries(TvShow seriesResult, string preferredCountryCode)
        {
            var series = new Series
            {
                Name = seriesResult.Name,
                OriginalTitle = seriesResult.OriginalName
            };

            series.SetProviderId(MetadataProvider.Tmdb, seriesResult.Id.ToString(CultureInfo.InvariantCulture));

            series.CommunityRating = Convert.ToSingle(seriesResult.VoteAverage);

            series.Overview = seriesResult.Overview;

            if (seriesResult.Networks is not null)
            {
                series.Studios = seriesResult.Networks.Select(i => i.Name).ToArray();
            }

            if (seriesResult.Genres is not null)
            {
                series.Genres = seriesResult.Genres.Select(i => i.Name).ToArray();
            }

            if (seriesResult.Keywords?.Results is not null)
            {
                for (var i = 0; i < seriesResult.Keywords.Results.Count; i++)
                {
                    series.AddTag(seriesResult.Keywords.Results[i].Name);
                }
            }

            series.HomePageUrl = seriesResult.Homepage;

            series.RunTimeTicks = seriesResult.EpisodeRunTime.Select(i => TimeSpan.FromMinutes(i).Ticks).FirstOrDefault();

            if (Emby.Naming.TV.TvParserHelpers.TryParseSeriesStatus(seriesResult.Status, out var seriesStatus))
            {
                series.Status = seriesStatus;
            }

            series.EndDate = seriesResult.LastAirDate;
            series.PremiereDate = seriesResult.FirstAirDate;

            var ids = seriesResult.ExternalIds;
            if (ids is not null)
            {
                series.TrySetProviderId(MetadataProvider.Imdb, ids.ImdbId);
                series.TrySetProviderId(MetadataProvider.TvRage, ids.TvrageId);
                series.TrySetProviderId(MetadataProvider.Tvdb, ids.TvdbId);
            }

            var contentRatings = seriesResult.ContentRatings.Results ?? new List<ContentRating>();

            var ourRelease = contentRatings.FirstOrDefault(c => string.Equals(c.Iso_3166_1, preferredCountryCode, StringComparison.OrdinalIgnoreCase));
            var usRelease = contentRatings.FirstOrDefault(c => string.Equals(c.Iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));
            var minimumRelease = contentRatings.FirstOrDefault();

            if (ourRelease is not null)
            {
                series.OfficialRating = TmdbUtils.BuildParentalRating(ourRelease.Iso_3166_1, ourRelease.Rating);
            }
            else if (usRelease is not null)
            {
                series.OfficialRating = usRelease.Rating;
            }
            else if (minimumRelease is not null)
            {
                series.OfficialRating = minimumRelease.Rating;
            }

            if (seriesResult.Videos?.Results is not null)
            {
                foreach (var video in seriesResult.Videos.Results)
                {
                    if (TmdbUtils.IsTrailerType(video))
                    {
                        series.AddTrailerUrl("https://www.youtube.com/watch?v=" + video.Key);
                    }
                }
            }

            return series;
        }

        private IEnumerable<PersonInfo> GetPersons(TvShow seriesResult)
        {
            if (seriesResult.Credits?.Cast is not null)
            {
                foreach (var actor in seriesResult.Credits.Cast.OrderBy(a => a.Order).Take(Plugin.Instance.Configuration.MaxCastMembers))
                {
                    var personInfo = new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character,
                        Type = PersonKind.Actor,
                        SortOrder = actor.Order,
                        ImageUrl = _tmdbClientManager.GetPosterUrl(actor.ProfilePath)
                    };

                    if (actor.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProvider.Tmdb, actor.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    yield return personInfo;
                }
            }

            if (seriesResult.Credits?.Crew is not null)
            {
                var keepTypes = new[]
                {
                    PersonType.Director,
                    PersonType.Writer,
                    PersonType.Producer
                };

                foreach (var person in seriesResult.Credits.Crew)
                {
                    // Normalize this
                    var type = TmdbUtils.MapCrewToPersonType(person);

                    if (!TmdbUtils.WantedCrewKinds.Contains(type)
                        && !TmdbUtils.WantedCrewTypes.Contains(person.Job ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new PersonInfo
                    {
                        Name = person.Name.Trim(),
                        Role = person.Job,
                        Type = type
                    };
                }
            }
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
