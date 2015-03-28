using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
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

        public LiveTvMediaSourceProvider(ILiveTvManager liveTvManager)
        {
            _liveTvManager = liveTvManager;
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
            var hasMediaSources = (IHasMediaSources)item;

            var sources = hasMediaSources.GetMediaSources(false)
                .ToList();

            foreach (var source in sources)
            {
                source.Type = MediaSourceType.Default;
                source.RequiresOpening = true;

                var openKeys = new List<string>();
                openKeys.Add(item.GetType().Name);
                openKeys.Add(item.Id.ToString("N"));
                source.OpenKey = string.Join("|", openKeys.ToArray());
            }

            return sources;
        }

        public async Task<MediaSourceInfo> OpenMediaSource(string openKey, CancellationToken cancellationToken)
        {
            var keys = openKey.Split(new[] { '|' }, 2);

            if (string.Equals(keys[0], typeof(LiveTvChannel).Name, StringComparison.OrdinalIgnoreCase))
            {
                return await _liveTvManager.GetChannelStream(keys[1], cancellationToken).ConfigureAwait(false);
            }

            return await _liveTvManager.GetRecordingStream(keys[1], cancellationToken).ConfigureAwait(false);
        }

        public Task CloseMediaSource(string closeKey, CancellationToken cancellationToken)
        {
            return _liveTvManager.CloseLiveStream(closeKey, cancellationToken);
        }
    }
}
