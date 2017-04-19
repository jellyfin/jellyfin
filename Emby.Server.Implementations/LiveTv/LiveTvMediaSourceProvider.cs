using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;

namespace Emby.Server.Implementations.LiveTv
{
    public class LiveTvMediaSourceProvider : IMediaSourceProvider
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerApplicationHost _appHost;

        public LiveTvMediaSourceProvider(ILiveTvManager liveTvManager, IJsonSerializer jsonSerializer, ILogManager logManager, IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder, IServerApplicationHost appHost)
        {
            _liveTvManager = liveTvManager;
            _jsonSerializer = jsonSerializer;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _appHost = appHost;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(IHasMediaSources item, CancellationToken cancellationToken)
        {
            var baseItem = (BaseItem)item;

            if (baseItem.SourceType == SourceType.LiveTV)
            {
                if (string.IsNullOrWhiteSpace(baseItem.Path))
                {
                    return GetMediaSourcesInternal(item, cancellationToken);
                }
            }

            return Task.FromResult<IEnumerable<MediaSourceInfo>>(new List<MediaSourceInfo>());
        }

        // Do not use a pipe here because Roku http requests to the server will fail, without any explicit error message.
        private const char StreamIdDelimeter = '_';
        private const string StreamIdDelimeterString = "_";

        private async Task<IEnumerable<MediaSourceInfo>> GetMediaSourcesInternal(IHasMediaSources item, CancellationToken cancellationToken)
        {
            IEnumerable<MediaSourceInfo> sources;

            var forceRequireOpening = false;

            try
            {
                if (item is ILiveTvRecording)
                {
                    sources = await _liveTvManager.GetRecordingMediaSources(item, cancellationToken)
                                .ConfigureAwait(false);
                }
                else
                {
                    sources = await _liveTvManager.GetChannelMediaSources(item, cancellationToken)
                                .ConfigureAwait(false);
                }
            }
            catch (NotImplementedException)
            {
                var hasMediaSources = (IHasMediaSources)item;

                sources = _mediaSourceManager.GetStaticMediaSources(hasMediaSources, false)
                   .ToList();

                forceRequireOpening = true;
            }

            var list = sources.ToList();
            var serverUrl = await _appHost.GetLocalApiUrl().ConfigureAwait(false);

            foreach (var source in list)
            {
                source.Type = MediaSourceType.Default;
                source.BufferMs = source.BufferMs ?? 1500;

                if (source.RequiresOpening || forceRequireOpening)
                {
                    source.RequiresOpening = true;
                }

                if (source.RequiresOpening)
                {
                    var openKeys = new List<string>();
                    openKeys.Add(item.GetType().Name);
                    openKeys.Add(item.Id.ToString("N"));
                    openKeys.Add(source.Id ?? string.Empty);
                    source.OpenToken = string.Join(StreamIdDelimeterString, openKeys.ToArray());
                }

                // Dummy this up so that direct play checks can still run
                if (string.IsNullOrEmpty(source.Path) && source.Protocol == MediaProtocol.Http)
                {
                    source.Path = serverUrl;
                }
            }

            _logger.Debug("MediaSources: {0}", _jsonSerializer.SerializeToString(list));

            return list;
        }

        public async Task<Tuple<MediaSourceInfo, IDirectStreamProvider>> OpenMediaSource(string openToken, CancellationToken cancellationToken)
        {
            MediaSourceInfo stream = null;
            const bool isAudio = false;

            var keys = openToken.Split(new[] { StreamIdDelimeter }, 3);
            var mediaSourceId = keys.Length >= 3 ? keys[2] : null;
            IDirectStreamProvider directStreamProvider = null;

            if (string.Equals(keys[0], typeof(LiveTvChannel).Name, StringComparison.OrdinalIgnoreCase))
            {
                var info = await _liveTvManager.GetChannelStream(keys[1], mediaSourceId, cancellationToken).ConfigureAwait(false);
                stream = info.Item1;
                directStreamProvider = info.Item2;
            }
            else
            {
                stream = await _liveTvManager.GetRecordingStream(keys[1], cancellationToken).ConfigureAwait(false);
            }

            try
            {
                if (!stream.SupportsProbing || stream.MediaStreams.Any(i => i.Index != -1))
                {
                    AddMediaInfo(stream, isAudio, cancellationToken);
                }
                else
                {
                    await new LiveStreamHelper(_mediaEncoder, _logger).AddMediaInfoWithProbe(stream, isAudio, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error probing live tv stream", ex);
            }

            _logger.Info("Live stream info: {0}", _jsonSerializer.SerializeToString(stream));
            return new Tuple<MediaSourceInfo, IDirectStreamProvider>(stream, directStreamProvider);
        }

        private void AddMediaInfo(MediaSourceInfo mediaSource, bool isAudio, CancellationToken cancellationToken)
        {
            mediaSource.DefaultSubtitleStreamIndex = null;

            // Null this out so that it will be treated like a live stream
            mediaSource.RunTimeTicks = null;

            var audioStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaBrowser.Model.Entities.MediaStreamType.Audio);

            if (audioStream == null || audioStream.Index == -1)
            {
                mediaSource.DefaultAudioStreamIndex = null;
            }
            else
            {
                mediaSource.DefaultAudioStreamIndex = audioStream.Index;
            }

            var videoStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaBrowser.Model.Entities.MediaStreamType.Video);
            if (videoStream != null)
            {
                if (!videoStream.BitRate.HasValue)
                {
                    var width = videoStream.Width ?? 1920;

                    if (width >= 3000)
                    {
                        videoStream.BitRate = 25000000;
                    }

                    else if (width >= 1900)
                    {
                        videoStream.BitRate = 15000000;
                    }

                    else if (width >= 1200)
                    {
                        videoStream.BitRate = 4000000;
                    }

                    else if (width >= 700)
                    {
                        videoStream.BitRate = 1500000;
                    }
                }
            }

            // Try to estimate this
            mediaSource.InferTotalBitrate();
        }

        public Task CloseMediaSource(string liveStreamId)
        {
            return _liveTvManager.CloseLiveStream(liveStreamId);
        }
    }
}
