using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    public class LiveTvMediaSourceProvider : IMediaSourceProvider
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;

        public LiveTvMediaSourceProvider(ILiveTvManager liveTvManager, IJsonSerializer jsonSerializer, ILogManager logManager, IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder)
        {
            _liveTvManager = liveTvManager;
            _jsonSerializer = jsonSerializer;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(IHasMediaSources item, CancellationToken cancellationToken)
        {
            var channelItem = item as ILiveTvItem;

            if (channelItem != null)
            {
                var hasMetadata = (IHasMetadata)channelItem;

                if (string.IsNullOrWhiteSpace(hasMetadata.Path))
                {
                    return GetMediaSourcesInternal(channelItem, cancellationToken);
                }
            }

            return Task.FromResult<IEnumerable<MediaSourceInfo>>(new List<MediaSourceInfo>());
        }

        private async Task<IEnumerable<MediaSourceInfo>> GetMediaSourcesInternal(ILiveTvItem item, CancellationToken cancellationToken)
        {
            IEnumerable<MediaSourceInfo> sources;

            try
            {
                if (item is ILiveTvRecording)
                {
                    sources = await _liveTvManager.GetRecordingMediaSources(item.Id.ToString("N"), cancellationToken)
                                .ConfigureAwait(false);
                }
                else
                {
                    sources = await _liveTvManager.GetChannelMediaSources(item.Id.ToString("N"), cancellationToken)
                                .ConfigureAwait(false);
                }
            }
            catch (NotImplementedException)
            {
                var hasMediaSources = (IHasMediaSources)item;

                sources = _mediaSourceManager.GetStaticMediaSources(hasMediaSources, false)
                   .ToList();
            }

            var list = sources.ToList();

            foreach (var source in list)
            {
                source.Type = MediaSourceType.Default;
                source.RequiresOpening = true;
                source.BufferMs = source.BufferMs ?? 1500;

                var openKeys = new List<string>();
                openKeys.Add(item.GetType().Name);
                openKeys.Add(item.Id.ToString("N"));
                openKeys.Add(source.Id ?? string.Empty);
                source.OpenToken = string.Join("|", openKeys.ToArray());
            }

            _logger.Debug("MediaSources: {0}", _jsonSerializer.SerializeToString(list));

            return list;
        }

        public async Task<MediaSourceInfo> OpenMediaSource(string openToken, CancellationToken cancellationToken)
        {
            MediaSourceInfo stream;
            const bool isAudio = false;

            var keys = openToken.Split(new[] { '|' }, 3);
            var mediaSourceId = keys.Length >= 3 ? keys[2] : null;

            if (string.Equals(keys[0], typeof(LiveTvChannel).Name, StringComparison.OrdinalIgnoreCase))
            {
                stream = await _liveTvManager.GetChannelStream(keys[1], mediaSourceId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                stream = await _liveTvManager.GetRecordingStream(keys[1], cancellationToken).ConfigureAwait(false);
            }

            try
            {
                await AddMediaInfo(stream, isAudio, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error probing live tv stream", ex);
            }

            return stream;
        }

        private async Task AddMediaInfo(MediaSourceInfo mediaSource, bool isAudio, CancellationToken cancellationToken)
        {
            var originalRuntime = mediaSource.RunTimeTicks;

            var info = await _mediaEncoder.GetMediaInfo(new MediaInfoRequest
            {
                InputPath = mediaSource.Path,
                Protocol = mediaSource.Protocol,
                MediaType = isAudio ? DlnaProfileType.Audio : DlnaProfileType.Video,
                ExtractChapters = false

            }, cancellationToken).ConfigureAwait(false);

            mediaSource.Bitrate = info.Bitrate;
            mediaSource.Container = info.Container;
            mediaSource.Formats = info.Formats;
            mediaSource.MediaStreams = info.MediaStreams;
            mediaSource.RunTimeTicks = info.RunTimeTicks;
            mediaSource.Size = info.Size;
            mediaSource.Timestamp = info.Timestamp;
            mediaSource.Video3DFormat = info.Video3DFormat;
            mediaSource.VideoType = info.VideoType;

            mediaSource.DefaultSubtitleStreamIndex = null;

            // Null this out so that it will be treated like a live stream
            if (!originalRuntime.HasValue)
            {
                mediaSource.RunTimeTicks = null;
            }

            var audioStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == Model.Entities.MediaStreamType.Audio);

            if (audioStream == null || audioStream.Index == -1)
            {
                mediaSource.DefaultAudioStreamIndex = null;
            }
            else
            {
                mediaSource.DefaultAudioStreamIndex = audioStream.Index;
            }

            // Try to estimate this
            if (!mediaSource.Bitrate.HasValue)
            {
                var videoStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == Model.Entities.MediaStreamType.Video);
                if (videoStream != null)
                {
                    var width = videoStream.Width ?? 1920;

                    if (width >= 1900)
                    {
                        mediaSource.Bitrate = 10000000;
                    }

                    else if (width >= 1260)
                    {
                        mediaSource.Bitrate = 6000000;
                    }

                    else if (width >= 700)
                    {
                        mediaSource.Bitrate = 4000000;
                    }
                }
            }
        }

        public Task CloseMediaSource(string liveStreamId, CancellationToken cancellationToken)
        {
            return _liveTvManager.CloseLiveStream(liveStreamId, cancellationToken);
        }
    }
}
