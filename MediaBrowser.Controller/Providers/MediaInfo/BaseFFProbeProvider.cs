using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Provides a base class for extracting media information through ffprobe
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseFFProbeProvider<T> : BaseFFMpegProvider<T>, IDisposable
        where T : BaseItem
    {
        protected BaseFFProbeProvider(ILogManager logManager, IServerConfigurationManager configurationManager) : base(logManager, configurationManager)
        {
        }

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

                var inputPath = isoMount == null ? 
                    Kernel.Instance.FFMpegManager.GetInputArgument(myItem) :
                    Kernel.Instance.FFMpegManager.GetInputArgument((Video)item, isoMount);

                var result = await Kernel.Instance.FFMpegManager.RunFFProbe(item, inputPath, item.DateModified, FFProbeCache, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                NormalizeFFProbeResult(result);

                cancellationToken.ThrowIfCancellationRequested();

                await Fetch(myItem, cancellationToken, result, isoMount).ConfigureAwait(false);

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
        private void NormalizeFFProbeResult(FFProbeResult result)
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
        protected abstract Task Fetch(T item, CancellationToken cancellationToken, FFProbeResult result, IIsoMount isoMount);

        /// <summary>
        /// Converts ffprobe stream info to our MediaStream class
        /// </summary>
        /// <param name="streamInfo">The stream info.</param>
        /// <param name="formatInfo">The format info.</param>
        /// <returns>MediaStream.</returns>
        protected MediaStream GetMediaStream(FFProbeMediaStreamInfo streamInfo, FFProbeMediaFormatInfo formatInfo)
        {
            var stream = new MediaStream
            {
                Codec = streamInfo.codec_name,
                Language = GetDictionaryValue(streamInfo.tags, "language"),
                Profile = streamInfo.profile,
                Index = streamInfo.index
            };

            if (streamInfo.codec_type.Equals("audio", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Audio;

                stream.Channels = streamInfo.channels;

                if (!string.IsNullOrEmpty(streamInfo.sample_rate))
                {
                    stream.SampleRate = int.Parse(streamInfo.sample_rate);
                }
            }
            else if (streamInfo.codec_type.Equals("subtitle", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Subtitle;
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
                    stream.BitRate = int.Parse(streamInfo.bit_rate);
                }
                else if (formatInfo != null && !string.IsNullOrEmpty(formatInfo.bit_rate))
                {
                    // If the stream info doesn't have a bitrate get the value from the media format info
                    stream.BitRate = int.Parse(formatInfo.bit_rate);
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
                    result = float.Parse(parts[0]) / float.Parse(parts[1]);
                }
                result = float.Parse(parts[0]);

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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                FFProbeCache.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
