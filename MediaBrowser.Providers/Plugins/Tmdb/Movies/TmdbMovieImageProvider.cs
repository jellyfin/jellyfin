#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using TMDbLib.Objects.Find;

namespace MediaBrowser.Providers.Plugins.Tmdb.Movies
{
    public class TmdbMovieImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        public TmdbMovieImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        public int Order => 0;

        public string Name => TmdbUtils.ProviderName;

        public bool Supports(BaseItem item)
        {
            return item is Movie || item is Trailer;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Logo
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var language = item.GetPreferredMetadataLanguage();

            var movieTmdbId = Convert.ToInt32(item.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            if (movieTmdbId <= 0)
            {
                var movieImdbId = item.GetProviderId(MetadataProvider.Imdb);
                if (string.IsNullOrEmpty(movieImdbId))
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }

                var movieResult = await _tmdbClientManager.FindByExternalIdAsync(movieImdbId, FindExternalSource.Imdb, language, cancellationToken).ConfigureAwait(false);
                if (movieResult?.MovieResults != null && movieResult.MovieResults.Count > 0)
                {
                    movieTmdbId = movieResult.MovieResults[0].Id;
                }
            }

            if (movieTmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var movie = await _tmdbClientManager
                .GetMovieAsync(movieTmdbId, null, null, cancellationToken)
                .ConfigureAwait(false);

            if (movie?.Images == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = movie.Images.Posters;
            var backdrops = movie.Images.Backdrops;
            var logos = movie.Images.Logos;
            var remoteImages = new List<RemoteImageInfo>(posters.Count + backdrops.Count + logos.Count);

            _tmdbClientManager.ConvertPostersToRemoteImageInfo(posters, language, remoteImages);
            _tmdbClientManager.ConvertBackdropsToRemoteImageInfo(backdrops, language, remoteImages);
            _tmdbClientManager.ConvertLogosToRemoteImageInfo(logos, language, remoteImages);

            return remoteImages;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
