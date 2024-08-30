using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Caching.Memory;
using TMDbLib.Client;
using TMDbLib.Objects.Collections;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// Manager class for abstracting the TMDb API client library.
    /// </summary>
    public class TmdbClientManager : IDisposable
    {
        private const int CacheDurationInHours = 1;

        private readonly IMemoryCache _memoryCache;
        private readonly TMDbClient _tmDbClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbClientManager"/> class.
        /// </summary>
        /// <param name="memoryCache">An instance of <see cref="IMemoryCache"/>.</param>
        public TmdbClientManager(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;

            var apiKey = Plugin.Instance.Configuration.TmdbApiKey;
            apiKey = string.IsNullOrEmpty(apiKey) ? TmdbUtils.ApiKey : apiKey;
            _tmDbClient = new TMDbClient(apiKey);

            // Not really interested in NotFoundException
            _tmDbClient.ThrowApiExceptions = false;
        }

        /// <summary>
        /// Gets a movie from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The movie's TMDb id.</param>
        /// <param name="language">The movie's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb movie or null if not found.</returns>
        public async Task<Movie?> GetMovieAsync(int tmdbId, string? language, string? imageLanguages, CancellationToken cancellationToken)
        {
            var key = $"movie-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out Movie? movie))
            {
                return movie;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var extraMethods = MovieMethods.Credits | MovieMethods.Releases | MovieMethods.Images | MovieMethods.Videos;
            if (!(Plugin.Instance?.Configuration.ExcludeTagsMovies).GetValueOrDefault())
            {
                extraMethods |= MovieMethods.Keywords;
            }

            movie = await _tmDbClient.GetMovieAsync(
                tmdbId,
                TmdbUtils.NormalizeLanguage(language),
                imageLanguages,
                extraMethods,
                cancellationToken).ConfigureAwait(false);

            if (movie is not null)
            {
                _memoryCache.Set(key, movie, TimeSpan.FromHours(CacheDurationInHours));
            }

            return movie;
        }

        /// <summary>
        /// Gets a collection from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The collection's TMDb id.</param>
        /// <param name="language">The collection's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb collection or null if not found.</returns>
        public async Task<Collection?> GetCollectionAsync(int tmdbId, string? language, string? imageLanguages, CancellationToken cancellationToken)
        {
            var key = $"collection-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out Collection? collection))
            {
                return collection;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            collection = await _tmDbClient.GetCollectionAsync(
                tmdbId,
                TmdbUtils.NormalizeLanguage(language),
                imageLanguages,
                CollectionMethods.Images,
                cancellationToken).ConfigureAwait(false);

            if (collection is not null)
            {
                _memoryCache.Set(key, collection, TimeSpan.FromHours(CacheDurationInHours));
            }

            return collection;
        }

        /// <summary>
        /// Gets a tv show from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The tv show's TMDb id.</param>
        /// <param name="language">The tv show's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv show information or null if not found.</returns>
        public async Task<TvShow?> GetSeriesAsync(int tmdbId, string? language, string? imageLanguages, CancellationToken cancellationToken)
        {
            var key = $"series-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out TvShow? series))
            {
                return series;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var extraMethods = TvShowMethods.Credits | TvShowMethods.Images | TvShowMethods.ExternalIds | TvShowMethods.Videos | TvShowMethods.ContentRatings | TvShowMethods.EpisodeGroups;
            if (!(Plugin.Instance?.Configuration.ExcludeTagsSeries).GetValueOrDefault())
            {
                extraMethods |= TvShowMethods.Keywords;
            }

            series = await _tmDbClient.GetTvShowAsync(
                tmdbId,
                language: TmdbUtils.NormalizeLanguage(language),
                includeImageLanguage: imageLanguages,
                extraMethods: extraMethods,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (series is not null)
            {
                _memoryCache.Set(key, series, TimeSpan.FromHours(CacheDurationInHours));
            }

            return series;
        }

        /// <summary>
        /// Gets a tv show episode group from the TMDb API based on the show id and the display order.
        /// </summary>
        /// <param name="tvShowId">The tv show's TMDb id.</param>
        /// <param name="displayOrder">The display order.</param>
        /// <param name="language">The tv show's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv show episode group information or null if not found.</returns>
        private async Task<TvGroupCollection?> GetSeriesGroupAsync(int tvShowId, string displayOrder, string? language, string? imageLanguages, CancellationToken cancellationToken)
        {
            TvGroupType? groupType =
                string.Equals(displayOrder, "originalAirDate", StringComparison.Ordinal) ? TvGroupType.OriginalAirDate :
                string.Equals(displayOrder, "absolute", StringComparison.Ordinal) ? TvGroupType.Absolute :
                string.Equals(displayOrder, "dvd", StringComparison.Ordinal) ? TvGroupType.DVD :
                string.Equals(displayOrder, "digital", StringComparison.Ordinal) ? TvGroupType.Digital :
                string.Equals(displayOrder, "storyArc", StringComparison.Ordinal) ? TvGroupType.StoryArc :
                string.Equals(displayOrder, "production", StringComparison.Ordinal) ? TvGroupType.Production :
                string.Equals(displayOrder, "tv", StringComparison.Ordinal) ? TvGroupType.TV :
                null;

            if (groupType is null)
            {
                return null;
            }

            var key = $"group-{tvShowId.ToString(CultureInfo.InvariantCulture)}-{displayOrder}-{language}";
            if (_memoryCache.TryGetValue(key, out TvGroupCollection? group))
            {
                return group;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var series = await GetSeriesAsync(tvShowId, language, imageLanguages, cancellationToken).ConfigureAwait(false);
            var episodeGroupId = series?.EpisodeGroups.Results.Find(g => g.Type == groupType)?.Id;

            if (episodeGroupId is null)
            {
                return null;
            }

            group = await _tmDbClient.GetTvEpisodeGroupsAsync(
                episodeGroupId,
                language: TmdbUtils.NormalizeLanguage(language),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (group is not null)
            {
                _memoryCache.Set(key, group, TimeSpan.FromHours(CacheDurationInHours));
            }

            return group;
        }

        /// <summary>
        /// Gets a tv season from the TMDb API based on the tv show's TMDb id.
        /// </summary>
        /// <param name="tvShowId">The tv season's TMDb id.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="language">The tv season's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv season information or null if not found.</returns>
        public async Task<TvSeason?> GetSeasonAsync(int tvShowId, int seasonNumber, string? language, string? imageLanguages, CancellationToken cancellationToken)
        {
            var key = $"season-{tvShowId.ToString(CultureInfo.InvariantCulture)}-s{seasonNumber.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out TvSeason? season))
            {
                return season;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            season = await _tmDbClient.GetTvSeasonAsync(
                tvShowId,
                seasonNumber,
                language: TmdbUtils.NormalizeLanguage(language),
                includeImageLanguage: imageLanguages,
                extraMethods: TvSeasonMethods.Credits | TvSeasonMethods.Images | TvSeasonMethods.ExternalIds | TvSeasonMethods.Videos,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (season is not null)
            {
                _memoryCache.Set(key, season, TimeSpan.FromHours(CacheDurationInHours));
            }

            return season;
        }

        /// <summary>
        /// Gets a movie from the TMDb API based on the tv show's TMDb id.
        /// </summary>
        /// <param name="tvShowId">The tv show's TMDb id.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <param name="displayOrder">The display order.</param>
        /// <param name="language">The episode's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv episode information or null if not found.</returns>
        public async Task<TvEpisode?> GetEpisodeAsync(int tvShowId, int seasonNumber, int episodeNumber, string displayOrder, string? language, string? imageLanguages, CancellationToken cancellationToken)
        {
            var key = $"episode-{tvShowId.ToString(CultureInfo.InvariantCulture)}-s{seasonNumber.ToString(CultureInfo.InvariantCulture)}e{episodeNumber.ToString(CultureInfo.InvariantCulture)}-{displayOrder}-{language}";
            if (_memoryCache.TryGetValue(key, out TvEpisode? episode))
            {
                return episode;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var group = await GetSeriesGroupAsync(tvShowId, displayOrder, language, imageLanguages, cancellationToken).ConfigureAwait(false);
            if (group is not null)
            {
                var season = group.Groups.Find(s => s.Order == seasonNumber);
                // Episode order starts at 0
                var ep = season?.Episodes.Find(e => e.Order == episodeNumber - 1);
                if (ep is not null)
                {
                    seasonNumber = ep.SeasonNumber;
                    episodeNumber = ep.EpisodeNumber;
                }
            }

            episode = await _tmDbClient.GetTvEpisodeAsync(
                tvShowId,
                seasonNumber,
                episodeNumber,
                language: TmdbUtils.NormalizeLanguage(language),
                includeImageLanguage: imageLanguages,
                extraMethods: TvEpisodeMethods.Credits | TvEpisodeMethods.Images | TvEpisodeMethods.ExternalIds | TvEpisodeMethods.Videos,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (episode is not null)
            {
                _memoryCache.Set(key, episode, TimeSpan.FromHours(CacheDurationInHours));
            }

            return episode;
        }

        /// <summary>
        /// Gets a person eg. cast or crew member from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="personTmdbId">The person's TMDb id.</param>
        /// <param name="language">The episode's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb person information or null if not found.</returns>
        public async Task<Person?> GetPersonAsync(int personTmdbId, string language, CancellationToken cancellationToken)
        {
            var key = $"person-{personTmdbId.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out Person? person))
            {
                return person;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            person = await _tmDbClient.GetPersonAsync(
                personTmdbId,
                TmdbUtils.NormalizeLanguage(language),
                PersonMethods.TvCredits | PersonMethods.MovieCredits | PersonMethods.Images | PersonMethods.ExternalIds,
                cancellationToken).ConfigureAwait(false);

            if (person is not null)
            {
                _memoryCache.Set(key, person, TimeSpan.FromHours(CacheDurationInHours));
            }

            return person;
        }

        /// <summary>
        /// Gets an item from the TMDb API based on its id from an external service eg. IMDb id, TvDb id.
        /// </summary>
        /// <param name="externalId">The item's external id.</param>
        /// <param name="source">The source of the id eg. IMDb.</param>
        /// <param name="language">The item's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb item or null if not found.</returns>
        public async Task<FindContainer?> FindByExternalIdAsync(
            string externalId,
            FindExternalSource source,
            string language,
            CancellationToken cancellationToken)
        {
            var key = $"find-{source.ToString()}-{externalId.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out FindContainer? result))
            {
                return result;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            result = await _tmDbClient.FindAsync(
                source,
                externalId,
                TmdbUtils.NormalizeLanguage(language),
                cancellationToken).ConfigureAwait(false);

            if (result is not null)
            {
                _memoryCache.Set(key, result, TimeSpan.FromHours(CacheDurationInHours));
            }

            return result;
        }

        /// <summary>
        /// Searches for a tv show using the TMDb API based on its name.
        /// </summary>
        /// <param name="name">The name of the tv show.</param>
        /// <param name="language">The tv show's language.</param>
        /// <param name="year">The year the tv show first aired.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv show information.</returns>
        public async Task<IReadOnlyList<SearchTv>> SearchSeriesAsync(string name, string language, int year = 0, CancellationToken cancellationToken = default)
        {
            var key = $"searchseries-{name}-{language}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchTv>? series) && series is not null)
            {
                return series.Results;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var searchResults = await _tmDbClient
                .SearchTvShowAsync(name, TmdbUtils.NormalizeLanguage(language), includeAdult: Plugin.Instance.Configuration.IncludeAdult, firstAirDateYear: year, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (searchResults.Results.Count > 0)
            {
                _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
            }

            return searchResults.Results;
        }

        /// <summary>
        /// Searches for a person based on their name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the person.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb person information.</returns>
        public async Task<IReadOnlyList<SearchPerson>> SearchPersonAsync(string name, CancellationToken cancellationToken)
        {
            var key = $"searchperson-{name}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchPerson>? person) && person is not null)
            {
                return person.Results;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var searchResults = await _tmDbClient
                .SearchPersonAsync(name, includeAdult: Plugin.Instance.Configuration.IncludeAdult, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (searchResults.Results.Count > 0)
            {
                _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
            }

            return searchResults.Results;
        }

        /// <summary>
        /// Searches for a movie based on its name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the movie.</param>
        /// <param name="language">The movie's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb movie information.</returns>
        public Task<IReadOnlyList<SearchMovie>> SearchMovieAsync(string name, string language, CancellationToken cancellationToken)
        {
            return SearchMovieAsync(name, 0, language, cancellationToken);
        }

        /// <summary>
        /// Searches for a movie based on its name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the movie.</param>
        /// <param name="year">The release year of the movie.</param>
        /// <param name="language">The movie's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb movie information.</returns>
        public async Task<IReadOnlyList<SearchMovie>> SearchMovieAsync(string name, int year, string language, CancellationToken cancellationToken)
        {
            var key = $"moviesearch-{name}-{year.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchMovie>? movies) && movies is not null)
            {
                return movies.Results;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var searchResults = await _tmDbClient
                .SearchMovieAsync(name, TmdbUtils.NormalizeLanguage(language), includeAdult: Plugin.Instance.Configuration.IncludeAdult, year: year, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (searchResults.Results.Count > 0)
            {
                _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
            }

            return searchResults.Results;
        }

        /// <summary>
        /// Searches for a collection based on its name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the collection.</param>
        /// <param name="language">The collection's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb collection information.</returns>
        public async Task<IReadOnlyList<SearchCollection>> SearchCollectionAsync(string name, string language, CancellationToken cancellationToken)
        {
            var key = $"collectionsearch-{name}-{language}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchCollection>? collections) && collections is not null)
            {
                return collections.Results;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            var searchResults = await _tmDbClient
                .SearchCollectionAsync(name, TmdbUtils.NormalizeLanguage(language), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (searchResults.Results.Count > 0)
            {
                _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
            }

            return searchResults.Results;
        }

        /// <summary>
        /// Handles bad path checking and builds the absolute url.
        /// </summary>
        /// <param name="size">The image size to fetch.</param>
        /// <param name="path">The relative URL of the image.</param>
        /// <returns>The absolute URL.</returns>
        private string? GetUrl(string? size, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return _tmDbClient.GetImageUrl(size, path, true).ToString();
        }

        /// <summary>
        /// Gets the absolute URL of the poster.
        /// </summary>
        /// <param name="posterPath">The relative URL of the poster.</param>
        /// <returns>The absolute URL.</returns>
        public string? GetPosterUrl(string posterPath)
        {
            return GetUrl(Plugin.Instance.Configuration.PosterSize, posterPath);
        }

        /// <summary>
        /// Gets the absolute URL of the profile image.
        /// </summary>
        /// <param name="actorProfilePath">The relative URL of the profile image.</param>
        /// <returns>The absolute URL.</returns>
        public string? GetProfileUrl(string actorProfilePath)
        {
            return GetUrl(Plugin.Instance.Configuration.ProfileSize, actorProfilePath);
        }

        /// <summary>
        /// Converts poster <see cref="ImageData"/>s into <see cref="RemoteImageInfo"/>s.
        /// </summary>
        /// <param name="images">The input images.</param>
        /// <param name="requestLanguage">The requested language.</param>
        /// <returns>The remote images.</returns>
        public IEnumerable<RemoteImageInfo> ConvertPostersToRemoteImageInfo(IReadOnlyList<ImageData> images, string requestLanguage)
            => ConvertToRemoteImageInfo(images, Plugin.Instance.Configuration.PosterSize, ImageType.Primary, requestLanguage);

        /// <summary>
        /// Converts backdrop <see cref="ImageData"/>s into <see cref="RemoteImageInfo"/>s.
        /// </summary>
        /// <param name="images">The input images.</param>
        /// <param name="requestLanguage">The requested language.</param>
        /// <returns>The remote images.</returns>
        public IEnumerable<RemoteImageInfo> ConvertBackdropsToRemoteImageInfo(IReadOnlyList<ImageData> images, string requestLanguage)
            => ConvertToRemoteImageInfo(images, Plugin.Instance.Configuration.BackdropSize, ImageType.Backdrop, requestLanguage);

        /// <summary>
        /// Converts logo <see cref="ImageData"/>s into <see cref="RemoteImageInfo"/>s.
        /// </summary>
        /// <param name="images">The input images.</param>
        /// <param name="requestLanguage">The requested language.</param>
        /// <returns>The remote images.</returns>
        public IEnumerable<RemoteImageInfo> ConvertLogosToRemoteImageInfo(IReadOnlyList<ImageData> images, string requestLanguage)
            => ConvertToRemoteImageInfo(images, Plugin.Instance.Configuration.LogoSize, ImageType.Logo, requestLanguage);

        /// <summary>
        /// Converts profile <see cref="ImageData"/>s into <see cref="RemoteImageInfo"/>s.
        /// </summary>
        /// <param name="images">The input images.</param>
        /// <param name="requestLanguage">The requested language.</param>
        /// <returns>The remote images.</returns>
        public IEnumerable<RemoteImageInfo> ConvertProfilesToRemoteImageInfo(IReadOnlyList<ImageData> images, string requestLanguage)
            => ConvertToRemoteImageInfo(images, Plugin.Instance.Configuration.ProfileSize, ImageType.Primary, requestLanguage);

        /// <summary>
        /// Converts still <see cref="ImageData"/>s into <see cref="RemoteImageInfo"/>s.
        /// </summary>
        /// <param name="images">The input images.</param>
        /// <param name="requestLanguage">The requested language.</param>
        /// <returns>The remote images.</returns>
        public IEnumerable<RemoteImageInfo> ConvertStillsToRemoteImageInfo(IReadOnlyList<ImageData> images, string requestLanguage)
            => ConvertToRemoteImageInfo(images, Plugin.Instance.Configuration.StillSize, ImageType.Primary, requestLanguage);

        /// <summary>
        /// Converts <see cref="ImageData"/>s into <see cref="RemoteImageInfo"/>s.
        /// </summary>
        /// <param name="images">The input images.</param>
        /// <param name="size">The size of the image to fetch.</param>
        /// <param name="type">The type of the image.</param>
        /// <param name="requestLanguage">The requested language.</param>
        /// <returns>The remote images.</returns>
        private IEnumerable<RemoteImageInfo> ConvertToRemoteImageInfo(IReadOnlyList<ImageData> images, string? size, ImageType type, string requestLanguage)
        {
            // sizes provided are for original resolution, don't store them when downloading scaled images
            var scaleImage = !string.Equals(size, "original", StringComparison.OrdinalIgnoreCase);

            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];

                var imageType = type;
                var language = TmdbUtils.AdjustImageLanguage(image.Iso_639_1, requestLanguage);

                // Return Backdrops with a language specified (it has text) as Thumb.
                if (imageType == ImageType.Backdrop && !string.IsNullOrEmpty(language))
                {
                    imageType = ImageType.Thumb;
                }

                yield return new RemoteImageInfo
                {
                    Url = GetUrl(size, image.FilePath),
                    CommunityRating = image.VoteAverage,
                    VoteCount = image.VoteCount,
                    Width = scaleImage ? null : image.Width,
                    Height = scaleImage ? null : image.Height,
                    Language = language,
                    ProviderName = TmdbUtils.ProviderName,
                    Type = imageType,
                    RatingType = RatingType.Score
                };
            }
        }

        private async Task EnsureClientConfigAsync()
        {
            if (!_tmDbClient.HasConfig)
            {
                var config = await _tmDbClient.GetConfigAsync().ConfigureAwait(false);
                ValidatePreferences(config);
            }
        }

        private static void ValidatePreferences(TMDbConfig config)
        {
            var imageConfig = config.Images;

            var pluginConfig = Plugin.Instance.Configuration;

            if (!imageConfig.PosterSizes.Contains(pluginConfig.PosterSize))
            {
                pluginConfig.PosterSize = imageConfig.PosterSizes[^1];
            }

            if (!imageConfig.BackdropSizes.Contains(pluginConfig.BackdropSize))
            {
                pluginConfig.BackdropSize = imageConfig.BackdropSizes[^1];
            }

            if (!imageConfig.LogoSizes.Contains(pluginConfig.LogoSize))
            {
                pluginConfig.LogoSize = imageConfig.LogoSizes[^1];
            }

            if (!imageConfig.ProfileSizes.Contains(pluginConfig.ProfileSize))
            {
                pluginConfig.ProfileSize = imageConfig.ProfileSizes[^1];
            }

            if (!imageConfig.StillSizes.Contains(pluginConfig.StillSize))
            {
                pluginConfig.StillSize = imageConfig.StillSizes[^1];
            }
        }

        /// <summary>
        /// Gets the <see cref="TMDbClient"/> configuration.
        /// </summary>
        /// <returns>The configuration.</returns>
        public async Task<TMDbConfig> GetClientConfiguration()
        {
            await EnsureClientConfigAsync().ConfigureAwait(false);

            return _tmDbClient.Config;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryCache?.Dispose();
                _tmDbClient?.Dispose();
            }
        }
    }
}
