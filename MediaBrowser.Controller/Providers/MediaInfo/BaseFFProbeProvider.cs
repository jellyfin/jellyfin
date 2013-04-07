using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Provides a base class for extracting media information through ffprobe
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseFFProbeProvider<T> : BaseFFMpegProvider<T>
        where T : BaseItem
    {
        protected BaseFFProbeProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IMediaEncoder mediaEncoder, IProtobufSerializer protobufSerializer)
            : base(logManager, configurationManager, mediaEncoder)
        {
            ProtobufSerializer = protobufSerializer;
        }

        protected readonly IProtobufSerializer ProtobufSerializer;
        
        /// <summary>
        /// Gets or sets the FF probe cache.
        /// </summary>
        /// <value>The FF probe cache.</value>
        protected FileSystemRepository FFProbeCache { get; set; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            FFProbeCache = new FileSystemRepository(Path.Combine(ConfigurationManager.ApplicationPaths.CachePath, CacheDirectoryName));
        }

        /// <summary>
        /// Gets the name of the cache directory.
        /// </summary>
        /// <value>The name of the cache directory.</value>
        protected virtual string CacheDirectoryName
        {
            get
            {
                return "ffmpeg-video-info";
            }
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            // Give this second priority
            // Give metadata xml providers a chance to fill in data first, so that we can skip this whenever possible
            get { return MetadataProviderPriority.Second; }
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var myItem = (T)item;

            var isoMount = await MountIsoIfNeeded(myItem, cancellationToken).ConfigureAwait(false);

            try
            {
                OnPreFetch(myItem, isoMount);

                var result = await GetMediaInfo(item, isoMount, item.DateModified, FFProbeCache, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                NormalizeFFProbeResult(result);

                cancellationToken.ThrowIfCancellationRequested();

                Fetch(myItem, cancellationToken, result, isoMount);

                cancellationToken.ThrowIfCancellationRequested();

                SetLastRefreshed(item, DateTime.UtcNow);
            }
            finally
            {
                if (isoMount != null)
                {
                    isoMount.Dispose();
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the media info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MediaInfoResult}.</returns>
        /// <exception cref="System.ArgumentNullException">inputPath
        /// or
        /// cache</exception>
        private async Task<MediaInfoResult> GetMediaInfo(BaseItem item, IIsoMount isoMount, DateTime lastDateModified, FileSystemRepository cache, CancellationToken cancellationToken)
        {
            if (cache == null)
            {
                throw new ArgumentNullException("cache");
            }

            // Put the ffmpeg version into the cache name so that it's unique per-version
            // We don't want to try and deserialize data based on an old version, which could potentially fail
            var resourceName = item.Id + "_" + lastDateModified.Ticks + "_" + MediaEncoder.Version;

            // Forumulate the cache file path
            var cacheFilePath = cache.GetResourcePath(resourceName, ".pb");

            cancellationToken.ThrowIfCancellationRequested();

            // Avoid File.Exists by just trying to deserialize
            try
            {
                return ProtobufSerializer.DeserializeFromFile<MediaInfoResult>(cacheFilePath);
            }
            catch (FileNotFoundException)
            {
                // Cache file doesn't exist
            }

            var type = InputType.AudioFile;
            var inputPath = isoMount == null ? new[] { item.Path } : new[] { isoMount.MountedPath };

            var video = item as Video;

            if (video != null)
            {
                inputPath = MediaEncoderHelpers.GetInputArgument(video, isoMount, out type);
            }

            var info = await MediaEncoder.GetMediaInfo(inputPath, type, cancellationToken).ConfigureAwait(false);

            ProtobufSerializer.SerializeToFile(info, cacheFilePath);

            return info;
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
        private void NormalizeFFProbeResult(MediaInfoResult result)
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
        /// Subclasses must set item values using this
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="result">The result.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>Task.</returns>
        protected abstract void Fetch(T item, CancellationToken cancellationToken, MediaInfoResult result, IIsoMount isoMount);

        /// <summary>
        /// Converts ffprobe stream info to our MediaStream class
        /// </summary>
        /// <param name="streamInfo">The stream info.</param>
        /// <param name="formatInfo">The format info.</param>
        /// <returns>MediaStream.</returns>
        protected MediaStream GetMediaStream(MediaStreamInfo streamInfo, MediaFormatInfo formatInfo)
        {
            var stream = new MediaStream
            {
                Codec = streamInfo.codec_name,
                Language = GetDictionaryValue(streamInfo.tags, "language"),
                Profile = streamInfo.profile,
                Level = streamInfo.level,
                Index = streamInfo.index
            };

            if (streamInfo.codec_type.Equals("audio", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Audio;

                stream.Channels = streamInfo.channels;

                if (!string.IsNullOrEmpty(streamInfo.sample_rate))
                {
                    stream.SampleRate = int.Parse(streamInfo.sample_rate, UsCulture);
                }
            }
            else if (streamInfo.codec_type.Equals("subtitle", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Subtitle;
            }
            else if (streamInfo.codec_type.Equals("data", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Data;
            }
            else
            {
                stream.Type = MediaStreamType.Video;

                stream.Width = streamInfo.width;
                stream.Height = streamInfo.height;
                stream.AspectRatio = streamInfo.display_aspect_ratio;

                stream.AverageFrameRate = GetFrameRate(streamInfo.avg_frame_rate);
                stream.RealFrameRate = GetFrameRate(streamInfo.r_frame_rate);
            }

            // Get stream bitrate
            if (stream.Type != MediaStreamType.Subtitle)
            {
                if (!string.IsNullOrEmpty(streamInfo.bit_rate))
                {
                    stream.BitRate = int.Parse(streamInfo.bit_rate, UsCulture);
                }
                else if (formatInfo != null && !string.IsNullOrEmpty(formatInfo.bit_rate))
                {
                    // If the stream info doesn't have a bitrate get the value from the media format info
                    stream.BitRate = int.Parse(formatInfo.bit_rate, UsCulture);
                }
            }

            if (streamInfo.disposition != null)
            {
                var isDefault = GetDictionaryValue(streamInfo.disposition, "default");
                var isForced = GetDictionaryValue(streamInfo.disposition, "forced");

                stream.IsDefault = string.Equals(isDefault, "1", StringComparison.OrdinalIgnoreCase);

                stream.IsForced = string.Equals(isForced, "1", StringComparison.OrdinalIgnoreCase);
            }

            return stream;
        }

        /// <summary>
        /// Gets a frame rate from a string value in ffprobe output
        /// This could be a number or in the format of 2997/125.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Nullable{System.Single}.</returns>
        private float? GetFrameRate(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split('/');

                float result;

                if (parts.Length == 2)
                {
                    result = float.Parse(parts[0], UsCulture) / float.Parse(parts[1], UsCulture);
                }
                else
                {
                    result = float.Parse(parts[0], UsCulture);
                }

                return float.IsNaN(result) ? (float?)null : result;
            }

            return null;
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
