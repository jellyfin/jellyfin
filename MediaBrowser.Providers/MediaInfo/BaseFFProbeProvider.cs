using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Provides a base class for extracting media information through ffprobe
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseFFProbeProvider<T> : BaseMetadataProvider
        where T : BaseItem, IHasMediaStreams
    {
        protected BaseFFProbeProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IMediaEncoder mediaEncoder, IJsonSerializer jsonSerializer)
            : base(logManager, configurationManager)
        {
            JsonSerializer = jsonSerializer;
            MediaEncoder = mediaEncoder;
        }

        protected readonly IMediaEncoder MediaEncoder;
        protected readonly IJsonSerializer JsonSerializer;

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item.LocationType == LocationType.FileSystem && item is T;
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            return item.DateModified;
        }

        /// <summary>
        /// The null mount task result
        /// </summary>
        protected readonly Task<IIsoMount> NullMountTaskResult = Task.FromResult<IIsoMount>(null);

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "ffmpeg20131209";
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
        /// Gets the media info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MediaInfoResult}.</returns>
        /// <exception cref="System.ArgumentNullException">inputPath
        /// or
        /// cache</exception>
        protected async Task<InternalMediaInfoResult> GetMediaInfo(BaseItem item, IIsoMount isoMount, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var type = InputType.File;
            var inputPath = isoMount == null ? new[] { item.Path } : new[] { isoMount.MountedPath };

            var video = item as Video;

            if (video != null)
            {
                inputPath = MediaEncoderHelpers.GetInputArgument(video.Path, video.LocationType == LocationType.Remote, video.VideoType, video.IsoType, isoMount, video.PlayableStreamFileNames, out type);
            }

            return await MediaEncoder.GetMediaInfo(inputPath, type, item is Audio, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Mounts the iso if needed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IsoMount.</returns>
        protected virtual Task<IIsoMount> MountIsoIfNeeded(T item, CancellationToken cancellationToken)
        {
            return NullMountTaskResult;
        }

        /// <summary>
        /// Called when [pre fetch].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="mount">The mount.</param>
        protected virtual void OnPreFetch(T item, IIsoMount mount)
        {

        }
    }
}
