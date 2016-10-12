using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelDynamicMediaSourceProvider : IMediaSourceProvider
    {
        private readonly ChannelManager _channelManager;

        public ChannelDynamicMediaSourceProvider(IChannelManager channelManager)
        {
            _channelManager = (ChannelManager)channelManager;
        }

        public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(IHasMediaSources item, CancellationToken cancellationToken)
        {
            var baseItem = (BaseItem) item;

            if (baseItem.SourceType == SourceType.Channel)
            {
                return _channelManager.GetDynamicMediaSources(baseItem, cancellationToken);
            }

            return Task.FromResult<IEnumerable<MediaSourceInfo>>(new List<MediaSourceInfo>());
        }

        public Task<Tuple<MediaSourceInfo, IDirectStreamProvider>> OpenMediaSource(string openToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CloseMediaSource(string liveStreamId)
        {
            throw new NotImplementedException();
        }
    }
}
