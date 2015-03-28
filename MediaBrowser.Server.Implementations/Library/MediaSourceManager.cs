using System.Collections.Concurrent;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Server.Implementations.LiveTv;

namespace MediaBrowser.Server.Implementations.Library
{
    public class MediaSourceManager : IMediaSourceManager, IDisposable
    {
        private readonly IItemRepository _itemRepo;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        private IMediaSourceProvider[] _providers;
        private readonly ILogger _logger;

        public MediaSourceManager(IItemRepository itemRepo, IUserManager userManager, ILibraryManager libraryManager, ILogger logger)
        {
            _itemRepo = itemRepo;
            _userManager = userManager;
            _libraryManager = libraryManager;
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

            if (string.IsNullOrWhiteSpace(userId))
            {
                mediaSources = hasMediaSources.GetMediaSources(enablePathSubstitution);
            }
            else
            {
                var user = _userManager.GetUserById(userId);
                mediaSources = GetStaticMediaSources(hasMediaSources, enablePathSubstitution, user);
            }

            var dynamicMediaSources = await GetDynamicMediaSources(hasMediaSources, cancellationToken).ConfigureAwait(false);

            var list = new List<MediaSourceInfo>();

            list.AddRange(mediaSources);

            foreach (var source in dynamicMediaSources)
            {
                if (source.Protocol == MediaProtocol.File)
                {
                    source.SupportsDirectStream = File.Exists(source.Path);

                    // TODO: Path substitution
                }
                else if (source.Protocol == MediaProtocol.Http)
                {
                    // TODO: Allow this when the source is plain http, e.g. not HLS or Mpeg Dash
                    source.SupportsDirectStream = false;
                }
                else
                {
                    source.SupportsDirectStream = false;
                }

                list.Add(source);
            }

            return SortMediaSources(list).Where(i => i.Type != MediaSourceType.Placeholder);
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
                var sources = await provider.GetMediaSources(item, cancellationToken).ConfigureAwait(false);
                var list = sources.ToList();

                foreach (var mediaSource in list)
                {
                    SetKeyProperties(provider, mediaSource);
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting media sources", ex);
                return new List<MediaSourceInfo>();
            }
        }

        private void SetKeyProperties(IMediaSourceProvider provider, MediaSourceInfo mediaSource)
        {
            var prefix = provider.GetType().FullName.GetMD5().ToString("N") + "|";

            if (!string.IsNullOrWhiteSpace(mediaSource.OpenKey) && !mediaSource.OpenKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                mediaSource.OpenKey = prefix + mediaSource.OpenKey;
            }

            if (!string.IsNullOrWhiteSpace(mediaSource.CloseKey) && !mediaSource.CloseKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                mediaSource.CloseKey = prefix + mediaSource.CloseKey;
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

        private readonly ConcurrentDictionary<string, string> _openStreams =
         new ConcurrentDictionary<string, string>();
        private readonly SemaphoreSlim _liveStreamSemaphore = new SemaphoreSlim(1, 1);
        public async Task<MediaSourceInfo> OpenMediaSource(string openKey, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var tuple = GetProvider(openKey);
                var provider = tuple.Item1;

                var mediaSource = await provider.OpenMediaSource(tuple.Item2, cancellationToken).ConfigureAwait(false);

                SetKeyProperties(provider, mediaSource);

                _openStreams.AddOrUpdate(mediaSource.CloseKey, mediaSource.CloseKey, (key, i) => mediaSource.CloseKey);
                
                return mediaSource;
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }
        }

        public async Task CloseMediaSource(string closeKey, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var tuple = GetProvider(closeKey);

                await tuple.Item1.OpenMediaSource(tuple.Item2, cancellationToken).ConfigureAwait(false);

                string removedKey;
                _openStreams.TryRemove(closeKey, out removedKey);
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }
        }

        private Tuple<IMediaSourceProvider, string> GetProvider(string key)
        {
            var keys = key.Split(new[] { '|' }, 2);

            var provider = _providers.FirstOrDefault(i => string.Equals(i.GetType().FullName.GetMD5().ToString("N"), keys[0], StringComparison.OrdinalIgnoreCase));

            return new Tuple<IMediaSourceProvider, string>(provider, keys[1]);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private readonly object _disposeLock = new object();
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                lock (_disposeLock)
                {
                    foreach (var key in _openStreams.Keys.ToList())
                    {
                        var task = CloseMediaSource(key, CancellationToken.None);

                        Task.WaitAll(task);
                    }

                    _openStreams.Clear();
                }
            }
        }
    }
}
