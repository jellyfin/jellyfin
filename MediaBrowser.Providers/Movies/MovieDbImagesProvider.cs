using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
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
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieDbImagesProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public MovieDbImagesProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
            _fileSystem = fileSystem;
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
            return SupportsItem(item);
        }

        public static bool SupportsItem(BaseItem item)
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
            if (item.HasImage(ImageType.Primary) && 
                item.BackdropImagePaths.Count >= ConfigurationManager.Configuration.MaxBackdrops &&
                !item.LockedFields.Contains(MetadataFields.Images))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var path = MovieDbProvider.Current.GetImagesDataFilePath(item);

            if (!string.IsNullOrEmpty(path))
            {
                var fileInfo = new FileInfo(path);

                if (fileInfo.Exists)
                {
                    return _fileSystem.GetLastWriteTimeUtc(fileInfo) > providerInfo.LastRefreshed;
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
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            if (!item.LockedFields.Contains(MetadataFields.Images))
            {
                var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualMovieDbImageProvider.ProviderName).ConfigureAwait(false);
                await ProcessImages(item, images.ToList(), cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        /// <summary>
        /// Processes the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task.</returns>
        private async Task ProcessImages(BaseItem item, List<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var eligiblePosters = images
                .Where(i => i.Type == ImageType.Primary)
                .ToList();

            //        poster
            if (eligiblePosters.Count > 0 && !item.HasImage(ImageType.Primary))
            {
                var poster = eligiblePosters[0];

                var url = poster.Url;

                var img = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken

                }).ConfigureAwait(false);

                await _providerManager.SaveImage(item, img, MimeTypes.GetMimeType(url), ImageType.Primary, null, url, cancellationToken)
                                    .ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var eligibleBackdrops = images
                .Where(i => i.Type == ImageType.Backdrop && i.Width.HasValue && i.Width.Value >= ConfigurationManager.Configuration.MinMovieBackdropDownloadWidth)
                .ToList();

            var backdropLimit = ConfigurationManager.Configuration.MaxBackdrops;

            // backdrops - only download if earlier providers didn't find any (fanart)
            if (eligibleBackdrops.Count > 0 && ConfigurationManager.Configuration.DownloadMovieImages.Backdrops && item.BackdropImagePaths.Count < backdropLimit)
            {
                for (var i = 0; i < eligibleBackdrops.Count; i++)
                {
                    var url = eligibleBackdrops[i].Url;

                    if (!item.ContainsImageWithSourceUrl(url))
                    {
                        var img = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                        {
                            Url = url,
                            CancellationToken = cancellationToken

                        }).ConfigureAwait(false);

                        await _providerManager.SaveImage(item, img, MimeTypes.GetMimeType(url), ImageType.Backdrop, null, url, cancellationToken)
                          .ConfigureAwait(false);
                    }

                    if (item.BackdropImagePaths.Count >= backdropLimit)
                    {
                        break;
                    }
                }
            }
        }
    }
}
