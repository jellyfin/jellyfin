using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
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

            return await MediaEncoder.GetMediaInfo(inputPath, type, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Normalizes the FF probe result.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void NormalizeFFProbeResult(InternalMediaInfoResult result)
        {
            if (result.format != null && result.format.tags != null)
            {
                result.format.tags = ConvertDictionaryToCaseInSensitive(result.format.tags);
            }

            if (result.streams != null)
            {
                // Convert all dictionaries to case insensitive
                foreach (var stream in result.streams)
                {
                    if (stream.tags != null)
                    {
                        stream.tags = ConvertDictionaryToCaseInSensitive(stream.tags);
                    }

                    if (stream.disposition != null)
                    {
                        stream.disposition = ConvertDictionaryToCaseInSensitive(stream.disposition);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a string from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        protected string GetDictionaryValue(Dictionary<string, string> tags, string key)
        {
            if (tags == null)
            {
                return null;
            }

            string val;

            tags.TryGetValue(key, out val);
            return val;
        }

        /// <summary>
        /// Gets an int from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        protected int? GetDictionaryNumericValue(Dictionary<string, string> tags, string key)
        {
            var val = GetDictionaryValue(tags, key);

            if (!string.IsNullOrEmpty(val))
            {
                int i;

                if (int.TryParse(val, out i))
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a DateTime from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.Nullable{DateTime}.</returns>
        protected DateTime? GetDictionaryDateTime(Dictionary<string, string> tags, string key)
        {
            var val = GetDictionaryValue(tags, key);

            if (!string.IsNullOrEmpty(val))
            {
                DateTime i;

                if (DateTime.TryParse(val, out i))
                {
                    return i.ToUniversalTime();
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a dictionary to case insensitive
        /// </summary>
        /// <param name="dict">The dict.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string> ConvertDictionaryToCaseInSensitive(Dictionary<string, string> dict)
        {
            return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }
    }
}
