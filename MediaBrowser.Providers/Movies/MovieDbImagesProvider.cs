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
using System.Globalization;
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
            get { return MetadataProviderPriority.Fifth; }
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
            if (item.HasImage(ImageType.Primary) && item.BackdropImagePaths.Count > 0)
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
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
            BaseProviderInfo data;

            if (!item.ProviderData.TryGetValue(Id, out data))
            {
                data = new BaseProviderInfo();
                item.ProviderData[Id] = data;
            }

            var images = await FetchImages(item, item.GetProviderId(MetadataProviders.Tmdb), cancellationToken).ConfigureAwait(false);

            var status = await ProcessImages(item, images, cancellationToken).ConfigureAwait(false);

            SetLastRefreshed(item, DateTime.UtcNow, status);
            return true;
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MovieImages}.</returns>
        private async Task<MovieImages> FetchImages(BaseItem item, string id, CancellationToken cancellationToken)
        {
            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = string.Format(GetImages, id, MovieDbProvider.ApiKey, item is BoxSet ? "collection" : "movie"),
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                return _jsonSerializer.DeserializeFromStream<MovieImages>(json);
            }
        }

        /// <summary>
        /// Processes the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task.</returns>
        protected virtual async Task<ProviderRefreshStatus> ProcessImages(BaseItem item, MovieImages images, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = ProviderRefreshStatus.Success;

            //        poster
            if (images.posters != null && images.posters.Count > 0 && !item.HasImage(ImageType.Primary))
            {
                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.base_url + "original";
                // get highest rated poster for our language

                var postersSortedByVote = images.posters.OrderByDescending(i => i.vote_average);

                var poster = postersSortedByVote.FirstOrDefault(p => p.iso_639_1 != null && p.iso_639_1.Equals(ConfigurationManager.Configuration.PreferredMetadataLanguage, StringComparison.OrdinalIgnoreCase));
                if (poster == null && !ConfigurationManager.Configuration.PreferredMetadataLanguage.Equals("en"))
                {
                    // couldn't find our specific language, find english (if that wasn't our language)
                    poster = postersSortedByVote.FirstOrDefault(p => p.iso_639_1 != null && p.iso_639_1.Equals("en", StringComparison.OrdinalIgnoreCase));
                }
                if (poster == null)
                {
                    //still couldn't find it - try highest rated null one
                    poster = postersSortedByVote.FirstOrDefault(p => p.iso_639_1 == null);
                }
                if (poster == null)
                {
                    //finally - just get the highest rated one
                    poster = postersSortedByVote.FirstOrDefault();
                }
                if (poster != null)
                {
                    var img = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                    {
                        Url = tmdbImageUrl + poster.file_path,
                        CancellationToken = cancellationToken

                    }).ConfigureAwait(false);

                    await _providerManager.SaveImage(item, img, MimeTypes.GetMimeType(poster.file_path), ImageType.Primary, null, cancellationToken)
                                        .ConfigureAwait(false);

                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // backdrops - only download if earlier providers didn't find any (fanart)
            if (images.backdrops != null && images.backdrops.Count > 0 && ConfigurationManager.Configuration.DownloadMovieImages.Backdrops && item.BackdropImagePaths.Count == 0)
            {
                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.base_url + "original";

                for (var i = 0; i < images.backdrops.Count; i++)
                {
                    var bdName = "backdrop" + (i == 0 ? "" : i.ToString(CultureInfo.InvariantCulture));

                    var hasLocalBackdrop = item.LocationType == LocationType.FileSystem && ConfigurationManager.Configuration.SaveLocalMeta ? item.HasLocalImage(bdName) : item.BackdropImagePaths.Count > i;

                    if (!hasLocalBackdrop)
                    {
                        var img = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                        {
                            Url = tmdbImageUrl + images.backdrops[i].file_path,
                            CancellationToken = cancellationToken

                        }).ConfigureAwait(false);

                        await _providerManager.SaveImage(item, img, MimeTypes.GetMimeType(images.backdrops[i].file_path), ImageType.Backdrop, item.BackdropImagePaths.Count, cancellationToken)
                          .ConfigureAwait(false);
                    }

                    if (item.BackdropImagePaths.Count >= ConfigurationManager.Configuration.MaxBackdrops)
                    {
                        break;
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Class Backdrop
        /// </summary>
        protected class Backdrop
        {
            /// <summary>
            /// Gets or sets the file_path.
            /// </summary>
            /// <value>The file_path.</value>
            public string file_path { get; set; }
            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            /// <value>The width.</value>
            public int width { get; set; }
            /// <summary>
            /// Gets or sets the height.
            /// </summary>
            /// <value>The height.</value>
            public int height { get; set; }
            /// <summary>
            /// Gets or sets the iso_639_1.
            /// </summary>
            /// <value>The iso_639_1.</value>
            public string iso_639_1 { get; set; }
            /// <summary>
            /// Gets or sets the aspect_ratio.
            /// </summary>
            /// <value>The aspect_ratio.</value>
            public double aspect_ratio { get; set; }
            /// <summary>
            /// Gets or sets the vote_average.
            /// </summary>
            /// <value>The vote_average.</value>
            public double vote_average { get; set; }
            /// <summary>
            /// Gets or sets the vote_count.
            /// </summary>
            /// <value>The vote_count.</value>
            public int vote_count { get; set; }
        }

        /// <summary>
        /// Class Poster
        /// </summary>
        protected class Poster
        {
            /// <summary>
            /// Gets or sets the file_path.
            /// </summary>
            /// <value>The file_path.</value>
            public string file_path { get; set; }
            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            /// <value>The width.</value>
            public int width { get; set; }
            /// <summary>
            /// Gets or sets the height.
            /// </summary>
            /// <value>The height.</value>
            public int height { get; set; }
            /// <summary>
            /// Gets or sets the iso_639_1.
            /// </summary>
            /// <value>The iso_639_1.</value>
            public string iso_639_1 { get; set; }
            /// <summary>
            /// Gets or sets the aspect_ratio.
            /// </summary>
            /// <value>The aspect_ratio.</value>
            public double aspect_ratio { get; set; }
            /// <summary>
            /// Gets or sets the vote_average.
            /// </summary>
            /// <value>The vote_average.</value>
            public double vote_average { get; set; }
            /// <summary>
            /// Gets or sets the vote_count.
            /// </summary>
            /// <value>The vote_count.</value>
            public int vote_count { get; set; }
        }

        /// <summary>
        /// Class MovieImages
        /// </summary>
        protected class MovieImages
        {
            /// <summary>
            /// Gets or sets the backdrops.
            /// </summary>
            /// <value>The backdrops.</value>
            public List<Backdrop> backdrops { get; set; }
            /// <summary>
            /// Gets or sets the posters.
            /// </summary>
            /// <value>The posters.</value>
            public List<Poster> posters { get; set; }
        }

    }
}
