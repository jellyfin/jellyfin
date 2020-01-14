#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

namespace Emby.Server.Implementations.Channels
{
    public class ChannelDynamicMediaSourceProvider : IMediaSourceProvider
    {
        private readonly ChannelManager _channelManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDynamicMediaSourceProvider"/> class.
        /// </summary>
        /// <param name="channelManager">The channel manager.</param>
        public ChannelDynamicMediaSourceProvider(IChannelManager channelManager)
        {
            _channelManager = (ChannelManager)channelManager;
        }

        /// <inheritdoc />
        public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            if (item.SourceType == SourceType.Channel)
            {
                return _channelManager.GetDynamicMediaSources(item, cancellationToken);
            }

            return Task.FromResult<IEnumerable<MediaSourceInfo>>(new List<MediaSourceInfo>());
        }

        /// <inheritdoc />
        public Task<ILiveStream> OpenMediaSource(string openToken, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
