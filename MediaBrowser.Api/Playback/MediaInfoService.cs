using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    [Route("/Items/{Id}/MediaInfo", "GET", Summary = "Gets live playback media info for an item")]
    public class GetLiveMediaInfo : IReturn<LiveMediaInfoResult>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Authenticated]
    public class MediaInfoService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IChannelManager _channelManager;
        private readonly IUserManager _userManager;

        public MediaInfoService(ILibraryManager libraryManager, IChannelManager channelManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _channelManager = channelManager;
            _userManager = userManager;
        }

        public async Task<object> Get(GetLiveMediaInfo request)
        {
            var item = _libraryManager.GetItemById(request.Id);
            IEnumerable<MediaSourceInfo> mediaSources;

            var channelItem = item as IChannelMediaItem;
            var user = _userManager.GetUserById(request.UserId);

            if (channelItem != null)
            {
                mediaSources = await _channelManager.GetChannelItemMediaSources(request.Id, true, CancellationToken.None)
                        .ConfigureAwait(false);
            }
            else
            {
                var hasMediaSources = (IHasMediaSources)item;

                if (user == null)
                {
                    mediaSources = hasMediaSources.GetMediaSources(true);
                }
                else
                {
                    mediaSources = hasMediaSources.GetMediaSources(true, user);
                }
            }

            return ToOptimizedResult(new LiveMediaInfoResult
            {
                MediaSources = mediaSources.ToList()
            });
        }
    }
}
