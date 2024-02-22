#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.IO;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv
{
    public class LiveTvMediaSourceProvider : IMediaSourceProvider
    {
        // Do not use a pipe here because Roku http requests to the server will fail, without any explicit error message.
        private const char StreamIdDelimiter = '_';

        private readonly ILogger<LiveTvMediaSourceProvider> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IRecordingsManager _recordingsManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILiveTvService[] _services;

        public LiveTvMediaSourceProvider(
            ILogger<LiveTvMediaSourceProvider> logger,
            IServerApplicationHost appHost,
            IRecordingsManager recordingsManager,
            IMediaSourceManager mediaSourceManager,
            ILibraryManager libraryManager,
            IEnumerable<ILiveTvService> services)
        {
            _logger = logger;
            _appHost = appHost;
            _recordingsManager = recordingsManager;
            _mediaSourceManager = mediaSourceManager;
            _libraryManager = libraryManager;
            _services = services.ToArray();
        }

        public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            if (item.SourceType == SourceType.LiveTV)
            {
                var activeRecordingInfo = _recordingsManager.GetActiveRecordingInfo(item.Path);

                if (string.IsNullOrEmpty(item.Path) || activeRecordingInfo is not null)
                {
                    return GetMediaSourcesInternal(item, activeRecordingInfo, cancellationToken);
                }
            }

            return Task.FromResult(Enumerable.Empty<MediaSourceInfo>());
        }

        private async Task<IEnumerable<MediaSourceInfo>> GetMediaSourcesInternal(BaseItem item, ActiveRecordingInfo activeRecordingInfo, CancellationToken cancellationToken)
        {
            IEnumerable<MediaSourceInfo> sources;

            var forceRequireOpening = false;

            try
            {
                if (activeRecordingInfo is not null)
                {
                    sources = await _mediaSourceManager.GetRecordingStreamMediaSources(activeRecordingInfo, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    sources = await GetChannelMediaSources(item, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (NotImplementedException)
            {
                sources = _mediaSourceManager.GetStaticMediaSources(item, false);

                forceRequireOpening = true;
            }

            var list = sources.ToList();

            foreach (var source in list)
            {
                source.Type = MediaSourceType.Default;
                source.BufferMs ??= 1500;

                if (source.RequiresOpening || forceRequireOpening)
                {
                    source.RequiresOpening = true;
                }

                if (source.RequiresOpening)
                {
                    var openKeys = new List<string>
                    {
                        item.GetType().Name,
                        item.Id.ToString("N", CultureInfo.InvariantCulture),
                        source.Id ?? string.Empty
                    };

                    source.OpenToken = string.Join(StreamIdDelimiter, openKeys);
                }

                // Dummy this up so that direct play checks can still run
                if (string.IsNullOrEmpty(source.Path) && source.Protocol == MediaProtocol.Http)
                {
                    source.Path = _appHost.GetApiUrlForLocalAccess();
                }
            }

            _logger.LogDebug("MediaSources: {@MediaSources}", list);

            return list;
        }

        /// <inheritdoc />
        public async Task<ILiveStream> OpenMediaSource(string openToken, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            var keys = openToken.Split(StreamIdDelimiter, 3);
            var mediaSourceId = keys.Length >= 3 ? keys[2] : null;

            var info = await GetChannelStream(keys[1], mediaSourceId, currentLiveStreams, cancellationToken).ConfigureAwait(false);
            var liveStream = info.Item2;

            return liveStream;
        }

        private static void Normalize(MediaSourceInfo mediaSource, ILiveTvService service, bool isVideo)
        {
            // Not all of the plugins are setting this
            mediaSource.IsInfiniteStream = true;

            if (mediaSource.MediaStreams.Count == 0)
            {
                if (isVideo)
                {
                    mediaSource.MediaStreams = new[]
                    {
                        new MediaStream
                        {
                            Type = MediaStreamType.Video,
                            // Set the index to -1 because we don't know the exact index of the video stream within the container
                            Index = -1,
                            // Set to true if unknown to enable deinterlacing
                            IsInterlaced = true
                        },
                        new MediaStream
                        {
                            Type = MediaStreamType.Audio,
                            // Set the index to -1 because we don't know the exact index of the audio stream within the container
                            Index = -1
                        }
                    };
                }
                else
                {
                    mediaSource.MediaStreams = new[]
                    {
                        new MediaStream
                        {
                            Type = MediaStreamType.Audio,
                            // Set the index to -1 because we don't know the exact index of the audio stream within the container
                            Index = -1
                        }
                    };
                }
            }

            // Clean some bad data coming from providers
            foreach (var stream in mediaSource.MediaStreams)
            {
                if (stream.BitRate is <= 0)
                {
                    stream.BitRate = null;
                }

                if (stream.Channels is <= 0)
                {
                    stream.Channels = null;
                }

                if (stream.AverageFrameRate is <= 0)
                {
                    stream.AverageFrameRate = null;
                }

                if (stream.RealFrameRate is <= 0)
                {
                    stream.RealFrameRate = null;
                }

                if (stream.Width is <= 0)
                {
                    stream.Width = null;
                }

                if (stream.Height is <= 0)
                {
                    stream.Height = null;
                }

                if (stream.SampleRate is <= 0)
                {
                    stream.SampleRate = null;
                }

                if (stream.Level is <= 0)
                {
                    stream.Level = null;
                }
            }

            var indexCount = mediaSource.MediaStreams.Select(i => i.Index).Distinct().Count();

            // If there are duplicate stream indexes, set them all to unknown
            if (indexCount != mediaSource.MediaStreams.Count)
            {
                foreach (var stream in mediaSource.MediaStreams)
                {
                    stream.Index = -1;
                }
            }

            // Set the total bitrate if not already supplied
            mediaSource.InferTotalBitrate();

            if (service is not DefaultLiveTvService)
            {
                mediaSource.SupportsTranscoding = true;
                foreach (var stream in mediaSource.MediaStreams)
                {
                    if (stream.Type == MediaStreamType.Video && string.IsNullOrWhiteSpace(stream.NalLengthSize))
                    {
                        stream.NalLengthSize = "0";
                    }

                    if (stream.Type == MediaStreamType.Video)
                    {
                        stream.IsInterlaced = true;
                    }
                }
            }
        }

        private async Task<Tuple<MediaSourceInfo, ILiveStream>> GetChannelStream(
            string id,
            string mediaSourceId,
            List<ILiveStream> currentLiveStreams,
            CancellationToken cancellationToken)
        {
            if (string.Equals(id, mediaSourceId, StringComparison.OrdinalIgnoreCase))
            {
                mediaSourceId = null;
            }

            var channel = (LiveTvChannel)_libraryManager.GetItemById(id);

            bool isVideo = channel.ChannelType == ChannelType.TV;
            var service = GetService(channel.ServiceName);
            _logger.LogInformation("Opening channel stream from {0}, external channel Id: {1}", service.Name, channel.ExternalId);

            MediaSourceInfo info;
#pragma warning disable CA1859 // TODO: Analyzer bug?
            ILiveStream liveStream;
#pragma warning restore CA1859
            if (service is ISupportsDirectStreamProvider supportsManagedStream)
            {
                liveStream = await supportsManagedStream.GetChannelStreamWithDirectStreamProvider(channel.ExternalId, mediaSourceId, currentLiveStreams, cancellationToken).ConfigureAwait(false);
                info = liveStream.MediaSource;
            }
            else
            {
                info = await service.GetChannelStream(channel.ExternalId, mediaSourceId, cancellationToken).ConfigureAwait(false);
                var openedId = info.Id;
                Func<Task> closeFn = () => service.CloseLiveStream(openedId, CancellationToken.None);

                liveStream = new ExclusiveLiveStream(info, closeFn);

                var startTime = DateTime.UtcNow;
                await liveStream.Open(cancellationToken).ConfigureAwait(false);
                var endTime = DateTime.UtcNow;
                _logger.LogInformation("Live stream opened after {0}ms", (endTime - startTime).TotalMilliseconds);
            }

            info.RequiresClosing = true;

            var idPrefix = service.GetType().FullName!.GetMD5().ToString("N", CultureInfo.InvariantCulture) + "_";

            info.LiveStreamId = idPrefix + info.Id;

            Normalize(info, service, isVideo);

            return new Tuple<MediaSourceInfo, ILiveStream>(info, liveStream);
        }

        private async Task<List<MediaSourceInfo>> GetChannelMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            var baseItem = (LiveTvChannel)item;
            var service = GetService(baseItem.ServiceName);

            var sources = await service.GetChannelStreamMediaSources(baseItem.ExternalId, cancellationToken).ConfigureAwait(false);
            if (sources.Count == 0)
            {
                throw new NotImplementedException();
            }

            foreach (var source in sources)
            {
                Normalize(source, service, baseItem.ChannelType == ChannelType.TV);
            }

            return sources;
        }

        private ILiveTvService GetService(string name)
            => _services.First(service => string.Equals(service.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
