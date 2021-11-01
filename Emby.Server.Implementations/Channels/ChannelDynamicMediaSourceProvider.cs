using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

namespace Emby.Server.Implementations.Channels
{
    /// <summary>
    /// A media source provider for channels.
    /// </summary>
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
            return item.SourceType == SourceType.Channel
                ? _channelManager.GetDynamicMediaSources(item, cancellationToken)
                : Task.FromResult(Enumerable.Empty<MediaSourceInfo>());
        }

        /// <inheritdoc />
        public Task<ILiveStream> OpenMediaSource(string openToken, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
