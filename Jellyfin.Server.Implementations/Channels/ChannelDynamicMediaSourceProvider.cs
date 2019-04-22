using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Channels;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Library;
using Jellyfin.Model.Dto;

namespace Jellyfin.Server.Implementations.Channels
{
    public class ChannelDynamicMediaSourceProvider : IMediaSourceProvider
    {
        private readonly ChannelManager _channelManager;

        public ChannelDynamicMediaSourceProvider(IChannelManager channelManager)
        {
            _channelManager = (ChannelManager)channelManager;
        }

        public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            if (item.SourceType == SourceType.Channel)
            {
                return _channelManager.GetDynamicMediaSources(item, cancellationToken);
            }

            return Task.FromResult<IEnumerable<MediaSourceInfo>>(new List<MediaSourceInfo>());
        }

        public Task<ILiveStream> OpenMediaSource(string openToken, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
