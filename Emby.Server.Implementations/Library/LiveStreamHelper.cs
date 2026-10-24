#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library
{
    public class LiveStreamHelper
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        public LiveStreamHelper(IMediaEncoder mediaEncoder, ILogger logger, IApplicationPaths appPaths)
        {
            _mediaEncoder = mediaEncoder;
            _logger = logger;
            _appPaths = appPaths;
        }

        public async Task AddMediaInfoWithProbe(MediaSourceInfo mediaSource, bool isAudio, string? cacheKey, bool addProbeDelay, CancellationToken cancellationToken)
        {
            var originalRuntime = mediaSource.RunTimeTicks;

            var now = DateTime.UtcNow;

            MediaInfo? mediaInfo = null;
            var cacheFilePath = string.IsNullOrEmpty(cacheKey) ? null : Path.Combine(_appPaths.CachePath, "mediainfo", cacheKey.GetMD5().ToString("N", CultureInfo.InvariantCulture) + ".json");

            if (cacheFilePath is not null)
            {
                try
                {
                    FileStream jsonStream = AsyncFile.OpenRead(cacheFilePath);

                    await using (jsonStream.ConfigureAwait(false))
                    {
                        mediaInfo = await JsonSerializer.DeserializeAsync<MediaInfo>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);
                        // _logger.LogDebug("Found cached media info");
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogDebug(ex, "Could not open cached media info");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error opening cached media info");
                }
            }

            if (mediaInfo is not null && IsIncompleteLiveProbe(mediaInfo))
            {
                _logger.LogInformation("Discarding cached live stream probe because video size or audio sample rate is missing");
                mediaInfo = null;
                TryDelete(cacheFilePath);
            }

            if (mediaInfo is null)
            {
                if (addProbeDelay)
                {
                    var delayMs = mediaSource.AnalyzeDurationMs ?? 0;
                    delayMs = Math.Max(3000, delayMs);
                    _logger.LogInformation("Waiting {0}ms before probing the live stream", delayMs);
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }

                mediaSource.AnalyzeDurationMs = 3000;

                mediaInfo = await _mediaEncoder.GetMediaInfo(
                    new MediaInfoRequest
                    {
                        MediaSource = mediaSource,
                        MediaType = isAudio ? DlnaProfileType.Audio : DlnaProfileType.Video,
                        ExtractChapters = false
                    },
                    cancellationToken).ConfigureAwait(false);

                if (cacheFilePath is not null && !IsIncompleteLiveProbe(mediaInfo))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFilePath) ?? throw new InvalidOperationException("Path can't be a root directory."));
                    FileStream createStream = AsyncFile.OpenWrite(cacheFilePath);
                    await using (createStream.ConfigureAwait(false))
                    {
                        await JsonSerializer.SerializeAsync(createStream, mediaInfo, _jsonOptions, cancellationToken).ConfigureAwait(false);
                    }

                    _logger.LogDebug("Saved media info to {0}", cacheFilePath);
                }
            }

            var mediaStreams = mediaInfo.MediaStreams;

            if (!string.IsNullOrEmpty(cacheKey))
            {
                var newList = new List<MediaStream>();
                newList.AddRange(mediaStreams.Where(i => i.Type == MediaStreamType.Video).Take(1));
                newList.AddRange(mediaStreams.Where(i => i.Type == MediaStreamType.Audio).Take(1));

                foreach (var stream in newList)
                {
                    stream.Index = -1;
                    stream.Language = null;
                }

                mediaStreams = newList;
            }

            _logger.LogInformation("Live tv media info probe took {0} seconds", (DateTime.UtcNow - now).TotalSeconds.ToString(CultureInfo.InvariantCulture));

            mediaSource.Bitrate = mediaInfo.Bitrate;
            mediaSource.Container = mediaInfo.Container;
            mediaSource.Formats = mediaInfo.Formats;
            mediaSource.MediaStreams = mediaStreams;
            mediaSource.RunTimeTicks = mediaInfo.RunTimeTicks;
            mediaSource.Size = mediaInfo.Size;
            mediaSource.Timestamp = mediaInfo.Timestamp;
            mediaSource.Video3DFormat = mediaInfo.Video3DFormat;
            mediaSource.VideoType = mediaInfo.VideoType;

            mediaSource.DefaultSubtitleStreamIndex = null;

            // Null this out so that it will be treated like a live stream
            if (!originalRuntime.HasValue)
            {
                mediaSource.RunTimeTicks = null;
            }

            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            if (audioStream is null || audioStream.Index == -1)
            {
                mediaSource.DefaultAudioStreamIndex = null;
            }
            else
            {
                mediaSource.DefaultAudioStreamIndex = audioStream.Index;
            }

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);
            if (videoStream is not null)
            {
                if (!videoStream.BitRate.HasValue)
                {
                    var width = videoStream.Width ?? 1920;

                    if (width >= 3000)
                    {
                        videoStream.BitRate = 30000000;
                    }
                    else if (width >= 1900)
                    {
                        videoStream.BitRate = 20000000;
                    }
                    else if (width >= 1200)
                    {
                        videoStream.BitRate = 8000000;
                    }
                    else if (width >= 700)
                    {
                        videoStream.BitRate = 2000000;
                    }
                }

                // This is coming up false and preventing stream copy
                videoStream.IsAVC = null;

                if (videoStream.Width is not > 0 || videoStream.Height is not > 0)
                {
                    _logger.LogInformation("Live stream video size is unknown. Transcoding instead of copying the stream");
                    mediaSource.SupportsDirectPlay = false;
                    mediaSource.SupportsDirectStream = false;
                }
            }

            mediaSource.AnalyzeDurationMs = 3000;

            // Try to estimate this
            mediaSource.InferTotalBitrate(true);
        }

        public Task AddMediaInfoWithProbe(MediaSourceInfo mediaSource, bool isAudio, bool addProbeDelay, CancellationToken cancellationToken)
        {
            return AddMediaInfoWithProbe(mediaSource, isAudio, null, addProbeDelay, cancellationToken);
        }

        /// <summary>
        /// A live probe that missed the video parameter sets reports 0×0, and one that missed the
        /// audio header reports a sample rate of 0. Caching either result makes later playback copy
        /// a stream the player cannot start.
        /// </summary>
        /// <param name="mediaInfo">The probed media information.</param>
        /// <returns>Whether the probe is incomplete.</returns>
        internal static bool IsIncompleteLiveProbe(MediaInfo? mediaInfo)
        {
            var video = mediaInfo?.MediaStreams?.FirstOrDefault(stream => stream.Type == MediaStreamType.Video);
            if (video is not null && (video.Width is not > 0 || video.Height is not > 0))
            {
                return true;
            }

            var audio = mediaInfo?.MediaStreams?.FirstOrDefault(stream => stream.Type == MediaStreamType.Audio);
            return audio?.SampleRate is 0;
        }

        private static void TryDelete(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }
}
