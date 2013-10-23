using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class MovieDbImagesProvider
    /// </summary>
    public class MovieDbImagesProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The get images
        /// </summary>
        private const string GetImages = @"http://api.themoviedb.org/3/{2}/{0}/images?api_key={1}";

        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieDbImagesProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        public MovieDbImagesProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IJsonSerializer jsonSerializer)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Fourth; }
        }

        /// <summary>
        /// Supports the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            // Don't support local trailers
            return item is Movie || item is BoxSet || item is MusicVideo;
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "3";
            }
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tmdb)))
            {
                return false;
            }

            // Don't refresh if we already have both poster and backdrop and we're not refreshing images
            if (item.HasImage(ImageType.Primary) && item.BackdropImagePaths.Count >= ConfigurationManager.Configuration.MaxBackdrops)
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tmdb)))
            {
                return false;
            }
            
            var path = MovieDbProvider.Current.GetDataFilePath(item, "default");

            if (!string.IsNullOrEmpty(path))
            {
                var fileInfo = new FileInfo(path);

                if (fileInfo.Exists)
                {
                    return fileInfo.LastWriteTimeUtc > providerInfo.LastRefreshed;
                }
            }

            return false;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(MetadataProviders.Tmdb);

            var status = ProviderRefreshStatus.Success;

            if (!string.IsNullOrEmpty(id))
            {
                var images = FetchImages(item);

                if (images != null)
                {
                    status = await ProcessImages(item, images, cancellationToken).ConfigureAwait(false);
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, status);
            return true;
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Task{MovieImages}.</returns>
        private MovieDbProvider.Images FetchImages(BaseItem item)
        {
            var path = MovieDbProvider.Current.GetDataFilePath(item, "default");

            if (!string.IsNullOrEmpty(path))
            {
                var fileInfo = new FileInfo(path);

                if (fileInfo.Exists)
                {
                    return _jsonSerializer.DeserializeFromFile<MovieDbProvider.CompleteMovieData>(path).images;
                }
            }

            return null;
        }

        /// <summary>
        /// Processes the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task.</returns>
        private async Task<ProviderRefreshStatus> ProcessImages(BaseItem item, MovieDbProvider.Images images, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = ProviderRefreshStatus.Success;

            var eligiblePosters = images.posters == null ?
                new List<MovieDbProvider.Poster>() :
                images.posters.Where(i => i.width >= ConfigurationManager.Configuration.MinMoviePosterWidth)
                .ToList();

            eligiblePosters = eligiblePosters.OrderByDescending(i => i.vote_average).ToList();

            //        poster
            if (eligiblePosters.Count > 0 && !item.HasImage(ImageType.Primary))
            {
                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.base_url + "original";
                // get highest rated poster for our language

                var poster = eligiblePosters.FirstOrDefault(p => string.Equals(p.iso_639_1, ConfigurationManager.Configuration.PreferredMetadataLanguage, StringComparison.OrdinalIgnoreCase));

                if (poster == null)
                {
                    // couldn't find our specific language, find english
                    poster = eligiblePosters.FirstOrDefault(p => string.Equals(p.iso_639_1, "en", StringComparison.OrdinalIgnoreCase));
                }

                if (poster == null)
                {
                    //still couldn't find it - try highest rated null one
                    poster = eligiblePosters.FirstOrDefault(p => p.iso_639_1 == null);
                }

                if (poster == null)
                {
                    //finally - just get the highest rated one
                    poster = eligiblePosters.FirstOrDefault();
                }

                if (poster != null)
                {
                    var url = tmdbImageUrl + poster.file_path;

                    var img = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = cancellationToken

                    }).ConfigureAwait(false);

                    await _providerManager.SaveImage(item, img, MimeTypes.GetMimeType(poster.file_path), ImageType.Primary, null, url, cancellationToken)
                                        .ConfigureAwait(false);

                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var eligibleBackdrops = images.backdrops == null ? new List<MovieDbProvider.Backdrop>() :
                images.backdrops.Where(i => i.width >= ConfigurationManager.Configuration.MinMovieBackdropWidth)
                .ToList();

            var backdropLimit = ConfigurationManager.Configuration.MaxBackdrops;

            // backdrops - only download if earlier providers didn't find any (fanart)
            if (eligibleBackdrops.Count > 0 && ConfigurationManager.Configuration.DownloadMovieImages.Backdrops && item.BackdropImagePaths.Count < backdropLimit)
            {
                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.base_url + "original";

                for (var i = 0; i < eligibleBackdrops.Count; i++)
                {
                    var url = tmdbImageUrl + eligibleBackdrops[i].file_path;

                    if (!item.ContainsImageWithSourceUrl(url))
                    {
                        var img = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                        {
                            Url = url,
                            CancellationToken = cancellationToken

                        }).ConfigureAwait(false);

                        await _providerManager.SaveImage(item, img, MimeTypes.GetMimeType(eligibleBackdrops[i].file_path), ImageType.Backdrop, item.BackdropImagePaths.Count, url, cancellationToken)
                          .ConfigureAwait(false);
                    }

                    if (item.BackdropImagePaths.Count >= backdropLimit)
                    {
                        break;
                    }
                }
            }

            return status;
        }
    }
}
