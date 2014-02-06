using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class AudioImageProvider : IDynamicImageProvider, IHasChangeMonitor
    {
        private readonly IIsoManager _isoManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _config;

        public AudioImageProvider(IIsoManager isoManager, IMediaEncoder mediaEncoder, IServerConfigurationManager config)
        {
            _isoManager = isoManager;
            _mediaEncoder = mediaEncoder;
            _config = config;
        }

        /// <summary>
        /// The null mount task result
        /// </summary>
        protected readonly Task<IIsoMount> NullMountTaskResult = Task.FromResult<IIsoMount>(null);

        /// <summary>
        /// Mounts the iso if needed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IIsoMount}.</returns>
        protected Task<IIsoMount> MountIsoIfNeeded(Video item, CancellationToken cancellationToken)
        {
            if (item.VideoType == VideoType.Iso)
            {
                return _isoManager.Mount(item.Path, cancellationToken);
            }

            return NullMountTaskResult;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

        public Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var audio = (Audio)item;

            // Can't extract if we didn't find a video stream in the file
            if (!audio.HasEmbeddedImage)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetVideoImage((Audio)item, cancellationToken);
        }

        public async Task<DynamicImageResponse> GetVideoImage(Audio item, CancellationToken cancellationToken)
        {
            var stream = await _mediaEncoder.ExtractImage(new[] { item.Path }, InputType.File, true, null, null, cancellationToken).ConfigureAwait(false);

            return new DynamicImageResponse
            {
                Format = ImageFormat.Jpg,
                HasImage = true,
                Stream = stream
            };
        }

        public string Name
        {
            get { return "Embedded Image"; }
        }

        public bool Supports(IHasImages item)
        {
            return item.LocationType == LocationType.FileSystem && item is Audio;
        }

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            return item.DateModified > date;
        }
    }
}
