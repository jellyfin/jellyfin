using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    class ManualMovieDbImageProvider : IImageProvider
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerConfigurationManager _config;

        public ManualMovieDbImageProvider(IJsonSerializer jsonSerializer, IServerConfigurationManager config)
        {
            _jsonSerializer = jsonSerializer;
            _config = config;
        }

        public string Name
        {
            get { return "TheMovieDB"; }
        }

        public bool Supports(BaseItem item, ImageType imageType)
        {
            if (MovieDbImagesProvider.SupportsItem(item))
            {
                return imageType == ImageType.Primary || imageType == ImageType.Backdrop;
            }

            return false;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetAvailableImages(BaseItem item, ImageType imageType, CancellationToken cancellationToken)
        {
            var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var results = MovieDbImagesProvider.FetchImages(item, _jsonSerializer);

            var tmdbImageUrl = tmdbSettings.images.base_url + "original";

            if (imageType == ImageType.Primary)
            {
                var sources = GetPosters(results, item);

                return sources.Select(i => new RemoteImageInfo
                {
                    Url = tmdbImageUrl + i.file_path,
                    CommunityRating = i.vote_average,
                    VoteCount = i.vote_count,
                    Width = i.width,
                    Height = i.height,
                    Language = i.iso_639_1,
                    ProviderName = Name
                });
            }

            if (imageType == ImageType.Backdrop)
            {
                var sources = GetBackdrops(results, item);

                return sources.Select(i => new RemoteImageInfo
                {
                    Url = tmdbImageUrl + i.file_path,
                    CommunityRating = i.vote_average,
                    VoteCount = i.vote_count,
                    Width = i.width,
                    Height = i.height,
                    ProviderName = Name
                });
            }

            throw new ArgumentException("Unrecognized ImageType: " + imageType);
        }
        
        /// <summary>
        /// Gets the posters.
        /// </summary>
        /// <param name="images">The images.</param>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{MovieDbProvider.Poster}.</returns>
        public IEnumerable<MovieDbProvider.Poster> GetPosters(MovieDbProvider.Images images, BaseItem item)
        {
            var language = _config.Configuration.PreferredMetadataLanguage;

            var eligiblePosters = images.posters == null ?
                new List<MovieDbProvider.Poster>() :
                images.posters.Where(i => i.width >= _config.Configuration.MinMoviePosterWidth)
                .ToList();

            return eligiblePosters.OrderByDescending(i => i.vote_average)
                .ThenByDescending(i =>
                {
                    if (string.Equals(language, i.iso_639_1, StringComparison.OrdinalIgnoreCase))
                    {
                        return 3;
                    }
                    if (string.Equals("en", i.iso_639_1, StringComparison.OrdinalIgnoreCase))
                    {
                        return 2;
                    }
                    if (string.IsNullOrEmpty(i.iso_639_1))
                    {
                        return 1;
                    }
                    return 0;
                })
                .ToList();
        }

        /// <summary>
        /// Gets the backdrops.
        /// </summary>
        /// <param name="images">The images.</param>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{MovieDbProvider.Backdrop}.</returns>
        public IEnumerable<MovieDbProvider.Backdrop> GetBackdrops(MovieDbProvider.Images images, BaseItem item)
        {
            var eligibleBackdrops = images.backdrops == null ? new List<MovieDbProvider.Backdrop>() :
                images.backdrops.Where(i => i.width >= _config.Configuration.MinMovieBackdropWidth)
                .ToList();

            return eligibleBackdrops.OrderByDescending(i => i.vote_average);
        }
    }
}
