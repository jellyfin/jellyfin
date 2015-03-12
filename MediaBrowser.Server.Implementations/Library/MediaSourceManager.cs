using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    public class MediaSourceManager : IMediaSourceManager
    {
        private readonly IItemRepository _itemRepo;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IChannelManager _channelManager;

        private IMediaSourceProvider[] _providers;
        private readonly ILogger _logger;

        public MediaSourceManager(IItemRepository itemRepo, IUserManager userManager, ILibraryManager libraryManager, IChannelManager channelManager, ILogger logger)
        {
            _itemRepo = itemRepo;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _channelManager = channelManager;
            _logger = logger;
        }

        public void AddParts(IEnumerable<IMediaSourceProvider> providers)
        {
            _providers = providers.ToArray();
        }

        public IEnumerable<MediaStream> GetMediaStreams(MediaStreamQuery query)
        {
            var list = _itemRepo.GetMediaStreams(query)
                .ToList();

            foreach (var stream in list)
            {
                stream.SupportsExternalStream = StreamSupportsExternalStream(stream);
            }

            return list;
        }

        private bool StreamSupportsExternalStream(MediaStream stream)
        {
            if (stream.IsExternal)
            {
                return true;
            }

            if (stream.IsTextSubtitleStream)
            {
                return InternalTextStreamSupportsExternalStream(stream);
            }

            return false;
        }

        private bool InternalTextStreamSupportsExternalStream(MediaStream stream)
        {
            return true;
        }

        public IEnumerable<MediaStream> GetMediaStreams(string mediaSourceId)
        {
            var list = GetMediaStreams(new MediaStreamQuery
            {
                ItemId = new Guid(mediaSourceId)
            });

            return GetMediaStreamsForItem(list);
        }

        public IEnumerable<MediaStream> GetMediaStreams(Guid itemId)
        {
            var list = GetMediaStreams(new MediaStreamQuery
            {
                ItemId = itemId
            });

            return GetMediaStreamsForItem(list);
        }

        private IEnumerable<MediaStream> GetMediaStreamsForItem(IEnumerable<MediaStream> streams)
        {
            var list = streams.ToList();

            var subtitleStreams = list
                .Where(i => i.Type == MediaStreamType.Subtitle)
                .ToList();

            if (subtitleStreams.Count > 0)
            {
                var videoStream = list.FirstOrDefault(i => i.Type == MediaStreamType.Video);

                // This is abitrary but at some point it becomes too slow to extract subtitles on the fly
                // We need to learn more about when this is the case vs. when it isn't
                const int maxAllowedBitrateForExternalSubtitleStream = 10000000;

                var videoBitrate = videoStream == null ? maxAllowedBitrateForExternalSubtitleStream : videoStream.BitRate ?? maxAllowedBitrateForExternalSubtitleStream;

                foreach (var subStream in subtitleStreams)
                {
                    var supportsExternalStream = StreamSupportsExternalStream(subStream);

                    if (supportsExternalStream && videoBitrate >= maxAllowedBitrateForExternalSubtitleStream)
                    {
                        supportsExternalStream = false;
                    }

                    subStream.SupportsExternalStream = supportsExternalStream;
                }
            }

            return list;
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetPlayackMediaSources(string id, string userId, bool enablePathSubstitution, CancellationToken cancellationToken)
        {
            var item = _libraryManager.GetItemById(id);
            IEnumerable<MediaSourceInfo> mediaSources;

            var hasMediaSources = (IHasMediaSources)item;
            var channelItem = item as IChannelMediaItem;

            if (channelItem != null)
            {
                mediaSources = await _channelManager.GetChannelItemMediaSources(id, true, cancellationToken)
                        .ConfigureAwait(false);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    mediaSources = hasMediaSources.GetMediaSources(enablePathSubstitution);
                }
                else
                {
                    var user = _userManager.GetUserById(userId);
                    mediaSources = GetStaticMediaSources(hasMediaSources, enablePathSubstitution, user);
                }
            }

            var dynamicMediaSources = await GetDynamicMediaSources(hasMediaSources, cancellationToken).ConfigureAwait(false);

            var list = new List<MediaSourceInfo>();

            list.AddRange(mediaSources);

            foreach (var source in dynamicMediaSources)
            {
                source.SupportsTranscoding = false;
                list.Add(source);
            }

            return SortMediaSources(list);
        }

        private async Task<IEnumerable<MediaSourceInfo>> GetDynamicMediaSources(IHasMediaSources item, CancellationToken cancellationToken)
        {
            var tasks = _providers.Select(i => GetDynamicMediaSources(item, i, cancellationToken));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i.ToList());
        }

        private async Task<IEnumerable<MediaSourceInfo>> GetDynamicMediaSources(IHasMediaSources item, IMediaSourceProvider provider, CancellationToken cancellationToken)
        {
            try
            {
                return await provider.GetMediaSources(item, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting media sources", ex);
                return new List<MediaSourceInfo>();
            }
        }

        public Task<IEnumerable<MediaSourceInfo>> GetPlayackMediaSources(string id, bool enablePathSubstitution, CancellationToken cancellationToken)
        {
            return GetPlayackMediaSources(id, null, enablePathSubstitution, cancellationToken);
        }

        public IEnumerable<MediaSourceInfo> GetStaticMediaSources(IHasMediaSources item, bool enablePathSubstitution)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!(item is Video))
            {
                return item.GetMediaSources(enablePathSubstitution);
            }

            return item.GetMediaSources(enablePathSubstitution);
        }

        public IEnumerable<MediaSourceInfo> GetStaticMediaSources(IHasMediaSources item, bool enablePathSubstitution, User user)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!(item is Video))
            {
                return item.GetMediaSources(enablePathSubstitution);
            }

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var sources = item.GetMediaSources(enablePathSubstitution).ToList();

            foreach (var source in sources)
            {
                SetUserProperties(source, user);
            }

            return sources;
        }

        private void SetUserProperties(MediaSourceInfo source, User user)
        {
            var preferredAudio = string.IsNullOrEmpty(user.Configuration.AudioLanguagePreference)
            ? new string[] { }
            : new[] { user.Configuration.AudioLanguagePreference };

            var preferredSubs = string.IsNullOrEmpty(user.Configuration.SubtitleLanguagePreference)
                ? new List<string> { }
                : new List<string> { user.Configuration.SubtitleLanguagePreference };

            source.DefaultAudioStreamIndex = MediaStreamSelector.GetDefaultAudioStreamIndex(source.MediaStreams, preferredAudio, user.Configuration.PlayDefaultAudioTrack);

            var defaultAudioIndex = source.DefaultAudioStreamIndex;
            var audioLangage = defaultAudioIndex == null
                ? null
                : source.MediaStreams.Where(i => i.Type == MediaStreamType.Audio && i.Index == defaultAudioIndex).Select(i => i.Language).FirstOrDefault();

            source.DefaultSubtitleStreamIndex = MediaStreamSelector.GetDefaultSubtitleStreamIndex(source.MediaStreams,
                preferredSubs,
                user.Configuration.SubtitleMode,
                audioLangage);
        }

        private IEnumerable<MediaSourceInfo> SortMediaSources(IEnumerable<MediaSourceInfo> sources)
        {
            return sources.OrderBy(i =>
            {
                if (i.VideoType.HasValue && i.VideoType.Value == VideoType.VideoFile)
                {
                    return 0;
                }

                return 1;

            }).ThenBy(i => i.Video3DFormat.HasValue ? 1 : 0)
            .ThenByDescending(i =>
            {
                var stream = i.VideoStream;

                return stream == null || stream.Width == null ? 0 : stream.Width.Value;
            })
            .ToList();
        }

        public MediaSourceInfo GetStaticMediaSource(IHasMediaSources item, string mediaSourceId, bool enablePathSubstitution)
        {
            return GetStaticMediaSources(item, enablePathSubstitution).FirstOrDefault(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
