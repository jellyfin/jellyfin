using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
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

        public LiveTvMediaSourceProvider(ILiveTvManager liveTvManager, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _liveTvManager = liveTvManager;
            _jsonSerializer = jsonSerializer;
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

                sources = hasMediaSources.GetMediaSources(false)
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
                source.OpenToken = string.Join("|", openKeys.ToArray());
            }

            _logger.Debug("MediaSources: {0}", _jsonSerializer.SerializeToString(list));

            return list;
        }

        public async Task<MediaSourceInfo> OpenMediaSource(string openToken, CancellationToken cancellationToken)
        {
            var keys = openToken.Split(new[] { '|' }, 2);

            if (string.Equals(keys[0], typeof(LiveTvChannel).Name, StringComparison.OrdinalIgnoreCase))
            {
                return await _liveTvManager.GetChannelStream(keys[1], cancellationToken).ConfigureAwait(false);
            }

            return await _liveTvManager.GetRecordingStream(keys[1], cancellationToken).ConfigureAwait(false);
        }

        public Task CloseMediaSource(string liveStreamId, CancellationToken cancellationToken)
        {
            return _liveTvManager.CloseLiveStream(liveStreamId, cancellationToken);
        }
    }
}
