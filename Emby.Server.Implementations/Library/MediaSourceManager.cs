#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library
{
    public class MediaSourceManager : IMediaSourceManager, IDisposable
    {
        private readonly IItemRepository _itemRepo;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILocalizationManager _localizationManager;
        private readonly IApplicationPaths _appPaths;

        private IMediaSourceProvider[] _providers;

        public MediaSourceManager(
            IItemRepository itemRepo,
            IApplicationPaths applicationPaths,
            ILocalizationManager localizationManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ILogger<MediaSourceManager> logger,
            IJsonSerializer jsonSerializer,
            IFileSystem fileSystem,
            IUserDataManager userDataManager,
            IMediaEncoder mediaEncoder)
        {
            _itemRepo = itemRepo;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _userDataManager = userDataManager;
            _mediaEncoder = mediaEncoder;
            _localizationManager = localizationManager;
            _appPaths = applicationPaths;
        }

        public void AddParts(IEnumerable<IMediaSourceProvider> providers)
        {
            _providers = providers.ToArray();
        }

        public List<MediaStream> GetMediaStreams(MediaStreamQuery query)
        {
            var list = _itemRepo.GetMediaStreams(query);

            foreach (var stream in list)
            {
                stream.SupportsExternalStream = StreamSupportsExternalStream(stream);
            }

            return list;
        }

        private static bool StreamSupportsExternalStream(MediaStream stream)
        {
            if (stream.IsExternal)
            {
                return true;
            }

            if (stream.IsTextSubtitleStream)
            {
                return true;
            }

            return false;
        }

        public List<MediaStream> GetMediaStreams(string mediaSourceId)
        {
            var list = GetMediaStreams(new MediaStreamQuery
            {
                ItemId = new Guid(mediaSourceId)
            });

            return GetMediaStreamsForItem(list);
        }

        public List<MediaStream> GetMediaStreams(Guid itemId)
        {
            var list = GetMediaStreams(new MediaStreamQuery
            {
                ItemId = itemId
            });

            return GetMediaStreamsForItem(list);
        }

        private List<MediaStream> GetMediaStreamsForItem(List<MediaStream> streams)
        {
            foreach (var stream in streams)
            {
                if (stream.Type == MediaStreamType.Subtitle)
                {
                    stream.SupportsExternalStream = StreamSupportsExternalStream(stream);
                }
            }

            return streams;
        }

        /// <inheritdoc />
        public List<MediaAttachment> GetMediaAttachments(MediaAttachmentQuery query)
        {
            return _itemRepo.GetMediaAttachments(query);
        }

        /// <inheritdoc />
        public List<MediaAttachment> GetMediaAttachments(Guid itemId)
        {
            return GetMediaAttachments(new MediaAttachmentQuery
            {
                ItemId = itemId
            });
        }

        public async Task<List<MediaSourceInfo>> GetPlaybackMediaSources(BaseItem item, User user, bool allowMediaProbe, bool enablePathSubstitution, CancellationToken cancellationToken)
        {
            var mediaSources = GetStaticMediaSources(item, enablePathSubstitution, user);

            if (allowMediaProbe && mediaSources[0].Type != MediaSourceType.Placeholder && !mediaSources[0].MediaStreams.Any(i => i.Type == MediaStreamType.Audio || i.Type == MediaStreamType.Video))
            {
                await item.RefreshMetadata(
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        EnableRemoteContentProbe = true,
                        MetadataRefreshMode = MetadataRefreshMode.FullRefresh
                    },
                    cancellationToken).ConfigureAwait(false);

                mediaSources = GetStaticMediaSources(item, enablePathSubstitution, user);
            }

            var dynamicMediaSources = await GetDynamicMediaSources(item, cancellationToken).ConfigureAwait(false);

            var list = new List<MediaSourceInfo>();

            list.AddRange(mediaSources);

            foreach (var source in dynamicMediaSources)
            {
                if (user != null)
                {
                    SetDefaultAudioAndSubtitleStreamIndexes(item, source, user);
                }

                // Validate that this is actually possible
                if (source.SupportsDirectStream)
                {
                    source.SupportsDirectStream = SupportsDirectStream(source.Path, source.Protocol);
                }

                list.Add(source);
            }

            if (user != null)
            {
                foreach (var source in list)
                {
                    if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!user.Policy.EnableAudioPlaybackTranscoding)
                        {
                            source.SupportsTranscoding = false;
                        }
                    }
                }
            }

            return SortMediaSources(list).Where(i => i.Type != MediaSourceType.Placeholder).ToList();
        }

        public MediaProtocol GetPathProtocol(string path)
        {
            if (path.StartsWith("Rtsp", StringComparison.OrdinalIgnoreCase))
            {
                return MediaProtocol.Rtsp;
            }
            if (path.StartsWith("Rtmp", StringComparison.OrdinalIgnoreCase))
            {
                return MediaProtocol.Rtmp;
            }
            if (path.StartsWith("Http", StringComparison.OrdinalIgnoreCase))
            {
                return MediaProtocol.Http;
            }
            if (path.StartsWith("rtp", StringComparison.OrdinalIgnoreCase))
            {
                return MediaProtocol.Rtp;
            }
            if (path.StartsWith("ftp", StringComparison.OrdinalIgnoreCase))
            {
                return MediaProtocol.Ftp;
            }
            if (path.StartsWith("udp", StringComparison.OrdinalIgnoreCase))
            {
                return MediaProtocol.Udp;
            }

            return _fileSystem.IsPathFile(path) ? MediaProtocol.File : MediaProtocol.Http;
        }

        public bool SupportsDirectStream(string path, MediaProtocol protocol)
        {
            if (protocol == MediaProtocol.File)
            {
                return true;
            }

            if (protocol == MediaProtocol.Http)
            {
                if (path != null)
                {
                    if (path.IndexOf(".m3u", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        private async Task<IEnumerable<MediaSourceInfo>> GetDynamicMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            var tasks = _providers.Select(i => GetDynamicMediaSources(item, i, cancellationToken));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i.ToList());
        }

        private async Task<IEnumerable<MediaSourceInfo>> GetDynamicMediaSources(BaseItem item, IMediaSourceProvider provider, CancellationToken cancellationToken)
        {
            try
            {
                var sources = await provider.GetMediaSources(item, cancellationToken).ConfigureAwait(false);
                var list = sources.ToList();

                foreach (var mediaSource in list)
                {
                    mediaSource.InferTotalBitrate();

                    SetKeyProperties(provider, mediaSource);
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media sources");
                return new List<MediaSourceInfo>();
            }
        }

        private static void SetKeyProperties(IMediaSourceProvider provider, MediaSourceInfo mediaSource)
        {
            var prefix = provider.GetType().FullName.GetMD5().ToString("N", CultureInfo.InvariantCulture) + LiveStreamIdDelimeter;

            if (!string.IsNullOrEmpty(mediaSource.OpenToken) && !mediaSource.OpenToken.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                mediaSource.OpenToken = prefix + mediaSource.OpenToken;
            }

            if (!string.IsNullOrEmpty(mediaSource.LiveStreamId) && !mediaSource.LiveStreamId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                mediaSource.LiveStreamId = prefix + mediaSource.LiveStreamId;
            }
        }

        public async Task<MediaSourceInfo> GetMediaSource(BaseItem item, string mediaSourceId, string liveStreamId, bool enablePathSubstitution, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(liveStreamId))
            {
                return await GetLiveStream(liveStreamId, cancellationToken).ConfigureAwait(false);
            }

            var sources = await GetPlaybackMediaSources(item, null, false, enablePathSubstitution, cancellationToken).ConfigureAwait(false);

            return sources.FirstOrDefault(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));
        }

        public List<MediaSourceInfo> GetStaticMediaSources(BaseItem item, bool enablePathSubstitution, User user = null)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var hasMediaSources = (IHasMediaSources)item;

            var sources = hasMediaSources.GetMediaSources(enablePathSubstitution);

            if (user != null)
            {
                foreach (var source in sources)
                {
                    SetDefaultAudioAndSubtitleStreamIndexes(item, source, user);
                }
            }

            return sources;
        }

        private string[] NormalizeLanguage(string language)
        {
            if (language == null)
            {
                return Array.Empty<string>();
            }

            var culture = _localizationManager.FindLanguageInfo(language);
            if (culture != null)
            {
                return culture.ThreeLetterISOLanguageNames;
            }

            return new string[] { language };
        }

        private void SetDefaultSubtitleStreamIndex(MediaSourceInfo source, UserItemData userData, User user, bool allowRememberingSelection)
        {
            if (userData.SubtitleStreamIndex.HasValue && user.Configuration.RememberSubtitleSelections && user.Configuration.SubtitleMode != SubtitlePlaybackMode.None && allowRememberingSelection)
            {
                var index = userData.SubtitleStreamIndex.Value;
                // Make sure the saved index is still valid
                if (index == -1 || source.MediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && i.Index == index))
                {
                    source.DefaultSubtitleStreamIndex = index;
                    return;
                }
            }

            var preferredSubs = string.IsNullOrEmpty(user.Configuration.SubtitleLanguagePreference)
                ? Array.Empty<string>() : NormalizeLanguage(user.Configuration.SubtitleLanguagePreference);

            var defaultAudioIndex = source.DefaultAudioStreamIndex;
            var audioLangage = defaultAudioIndex == null
                ? null
                : source.MediaStreams.Where(i => i.Type == MediaStreamType.Audio && i.Index == defaultAudioIndex).Select(i => i.Language).FirstOrDefault();

            source.DefaultSubtitleStreamIndex = MediaStreamSelector.GetDefaultSubtitleStreamIndex(source.MediaStreams,
                preferredSubs,
                user.Configuration.SubtitleMode,
                audioLangage);

            MediaStreamSelector.SetSubtitleStreamScores(source.MediaStreams, preferredSubs,
                user.Configuration.SubtitleMode, audioLangage);
        }

        private void SetDefaultAudioStreamIndex(MediaSourceInfo source, UserItemData userData, User user, bool allowRememberingSelection)
        {
            if (userData.AudioStreamIndex.HasValue && user.Configuration.RememberAudioSelections && allowRememberingSelection)
            {
                var index = userData.AudioStreamIndex.Value;
                // Make sure the saved index is still valid
                if (source.MediaStreams.Any(i => i.Type == MediaStreamType.Audio && i.Index == index))
                {
                    source.DefaultAudioStreamIndex = index;
                    return;
                }
            }

            var preferredAudio = string.IsNullOrEmpty(user.Configuration.AudioLanguagePreference)
                ? Array.Empty<string>()
                : NormalizeLanguage(user.Configuration.AudioLanguagePreference);

            source.DefaultAudioStreamIndex = MediaStreamSelector.GetDefaultAudioStreamIndex(source.MediaStreams, preferredAudio, user.Configuration.PlayDefaultAudioTrack);
        }

        public void SetDefaultAudioAndSubtitleStreamIndexes(BaseItem item, MediaSourceInfo source, User user)
        {
            // Item would only be null if the app didn't supply ItemId as part of the live stream open request
            var mediaType = item == null ? MediaType.Video : item.MediaType;

            if (string.Equals(mediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                var userData = item == null ? new UserItemData() : _userDataManager.GetUserData(user, item);

                var allowRememberingSelection = item == null || item.EnableRememberingTrackSelections;

                SetDefaultAudioStreamIndex(source, userData, user, allowRememberingSelection);
                SetDefaultSubtitleStreamIndex(source, userData, user, allowRememberingSelection);
            }
            else if (string.Equals(mediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                var audio = source.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

                if (audio != null)
                {
                    source.DefaultAudioStreamIndex = audio.Index;
                }
            }
        }

        private static IEnumerable<MediaSourceInfo> SortMediaSources(IEnumerable<MediaSourceInfo> sources)
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

        private readonly Dictionary<string, ILiveStream> _openStreams = new Dictionary<string, ILiveStream>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _liveStreamSemaphore = new SemaphoreSlim(1, 1);

        public async Task<Tuple<LiveStreamResponse, IDirectStreamProvider>> OpenLiveStreamInternal(LiveStreamRequest request, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            MediaSourceInfo mediaSource;
            ILiveStream liveStream;

            try
            {
                var tuple = GetProvider(request.OpenToken);
                var provider = tuple.Item1;

                var currentLiveStreams = _openStreams.Values.ToList();

                liveStream = await provider.OpenMediaSource(tuple.Item2, currentLiveStreams, cancellationToken).ConfigureAwait(false);

                mediaSource = liveStream.MediaSource;

                // Validate that this is actually possible
                if (mediaSource.SupportsDirectStream)
                {
                    mediaSource.SupportsDirectStream = SupportsDirectStream(mediaSource.Path, mediaSource.Protocol);
                }

                SetKeyProperties(provider, mediaSource);

                _openStreams[mediaSource.LiveStreamId] = liveStream;
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }

            // TODO: Don't hardcode this
            const bool isAudio = false;

            try
            {
                if (mediaSource.MediaStreams.Any(i => i.Index != -1) || !mediaSource.SupportsProbing)
                {
                    AddMediaInfo(mediaSource, isAudio);
                }
                else
                {
                    // hack - these two values were taken from LiveTVMediaSourceProvider
                    string cacheKey = request.OpenToken;

                    await new LiveStreamHelper(_mediaEncoder, _logger, _jsonSerializer, _appPaths)
                        .AddMediaInfoWithProbe(mediaSource, isAudio, cacheKey, true, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error probing live tv stream");
                AddMediaInfo(mediaSource, isAudio);
            }

            // TODO: @bond Fix
            var json = _jsonSerializer.SerializeToString(mediaSource);
            _logger.LogInformation("Live stream opened: " + json);
            var clone = _jsonSerializer.DeserializeFromString<MediaSourceInfo>(json);

            if (!request.UserId.Equals(Guid.Empty))
            {
                var user = _userManager.GetUserById(request.UserId);
                var item = request.ItemId.Equals(Guid.Empty)
                    ? null
                    : _libraryManager.GetItemById(request.ItemId);
                SetDefaultAudioAndSubtitleStreamIndexes(item, clone, user);
            }

            return new Tuple<LiveStreamResponse, IDirectStreamProvider>(new LiveStreamResponse(clone), liveStream as IDirectStreamProvider);
        }

        private static void AddMediaInfo(MediaSourceInfo mediaSource, bool isAudio)
        {
            mediaSource.DefaultSubtitleStreamIndex = null;

            // Null this out so that it will be treated like a live stream
            if (mediaSource.IsInfiniteStream)
            {
                mediaSource.RunTimeTicks = null;
            }

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
                        videoStream.BitRate = 30000000;
                    }

                    else if (width >= 1900)
                    {
                        videoStream.BitRate = 20000000;
                    }

                    else if (width >= 1200)
                    {
                        videoStream.BitRate = 8000000;
                    }

                    else if (width >= 700)
                    {
                        videoStream.BitRate = 2000000;
                    }
                }
            }

            // Try to estimate this
            mediaSource.InferTotalBitrate();
        }

        public async Task<IDirectStreamProvider> GetDirectStreamProviderByUniqueId(string uniqueId, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var info = _openStreams.Values.FirstOrDefault(i =>
                {
                    var liveStream = i as ILiveStream;
                    if (liveStream != null)
                    {
                        return string.Equals(liveStream.UniqueId, uniqueId, StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                });

                return info as IDirectStreamProvider;
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }
        }

        public async Task<LiveStreamResponse> OpenLiveStream(LiveStreamRequest request, CancellationToken cancellationToken)
        {
            var result = await OpenLiveStreamInternal(request, cancellationToken).ConfigureAwait(false);
            return result.Item1;
        }

        public async Task<MediaSourceInfo> GetLiveStreamMediaInfo(string id, CancellationToken cancellationToken)
        {
            var liveStreamInfo = await GetLiveStreamInfo(id, cancellationToken).ConfigureAwait(false);

            var mediaSource = liveStreamInfo.MediaSource;

            if (liveStreamInfo is IDirectStreamProvider)
            {
                var info = await _mediaEncoder.GetMediaInfo(new MediaInfoRequest
                {
                    MediaSource = mediaSource,
                    ExtractChapters = false,
                    MediaType = DlnaProfileType.Video

                }, cancellationToken).ConfigureAwait(false);

                mediaSource.MediaStreams = info.MediaStreams;
                mediaSource.Container = info.Container;
                mediaSource.Bitrate = info.Bitrate;
            }

            return mediaSource;
        }

        public async Task AddMediaInfoWithProbe(MediaSourceInfo mediaSource, bool isAudio, string cacheKey, bool addProbeDelay, bool isLiveStream, CancellationToken cancellationToken)
        {
            var originalRuntime = mediaSource.RunTimeTicks;

            var now = DateTime.UtcNow;

            MediaInfo mediaInfo = null;
            var cacheFilePath = string.IsNullOrEmpty(cacheKey) ? null : Path.Combine(_appPaths.CachePath, "mediainfo", cacheKey.GetMD5().ToString("N", CultureInfo.InvariantCulture) + ".json");

            if (!string.IsNullOrEmpty(cacheKey))
            {
                try
                {
                    mediaInfo = _jsonSerializer.DeserializeFromFile<MediaInfo>(cacheFilePath);

                    //_logger.LogDebug("Found cached media info");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "_jsonSerializer.DeserializeFromFile threw an exception.");
                }
            }

            if (mediaInfo == null)
            {
                if (addProbeDelay)
                {
                    var delayMs = mediaSource.AnalyzeDurationMs ?? 0;
                    delayMs = Math.Max(3000, delayMs);
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }

                if (isLiveStream)
                {
                    mediaSource.AnalyzeDurationMs = 3000;
                }

                mediaInfo = await _mediaEncoder.GetMediaInfo(new MediaInfoRequest
                {
                    MediaSource = mediaSource,
                    MediaType = isAudio ? DlnaProfileType.Audio : DlnaProfileType.Video,
                    ExtractChapters = false

                }, cancellationToken).ConfigureAwait(false);

                if (cacheFilePath != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFilePath));
                    _jsonSerializer.SerializeToFile(mediaInfo, cacheFilePath);

                    //_logger.LogDebug("Saved media info to {0}", cacheFilePath);
                }
            }

            var mediaStreams = mediaInfo.MediaStreams;

            if (isLiveStream && !string.IsNullOrEmpty(cacheKey))
            {
                var newList = new List<MediaStream>();
                newList.AddRange(mediaStreams.Where(i => i.Type == MediaStreamType.Video).Take(1));
                newList.AddRange(mediaStreams.Where(i => i.Type == MediaStreamType.Audio).Take(1));

                foreach (var stream in newList)
                {
                    stream.Index = -1;
                    stream.Language = null;
                }

                mediaStreams = newList;
            }

            _logger.LogInformation("Live tv media info probe took {0} seconds", (DateTime.UtcNow - now).TotalSeconds.ToString(CultureInfo.InvariantCulture));

            mediaSource.Bitrate = mediaInfo.Bitrate;
            mediaSource.Container = mediaInfo.Container;
            mediaSource.Formats = mediaInfo.Formats;
            mediaSource.MediaStreams = mediaStreams;
            mediaSource.RunTimeTicks = mediaInfo.RunTimeTicks;
            mediaSource.Size = mediaInfo.Size;
            mediaSource.Timestamp = mediaInfo.Timestamp;
            mediaSource.Video3DFormat = mediaInfo.Video3DFormat;
            mediaSource.VideoType = mediaInfo.VideoType;

            mediaSource.DefaultSubtitleStreamIndex = null;

            if (isLiveStream)
            {
                // Null this out so that it will be treated like a live stream
                if (!originalRuntime.HasValue)
                {
                    mediaSource.RunTimeTicks = null;
                }
            }

            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            if (audioStream == null || audioStream.Index == -1)
            {
                mediaSource.DefaultAudioStreamIndex = null;
            }
            else
            {
                mediaSource.DefaultAudioStreamIndex = audioStream.Index;
            }

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);
            if (videoStream != null)
            {
                if (!videoStream.BitRate.HasValue)
                {
                    var width = videoStream.Width ?? 1920;

                    if (width >= 3000)
                    {
                        videoStream.BitRate = 30000000;
                    }

                    else if (width >= 1900)
                    {
                        videoStream.BitRate = 20000000;
                    }

                    else if (width >= 1200)
                    {
                        videoStream.BitRate = 8000000;
                    }

                    else if (width >= 700)
                    {
                        videoStream.BitRate = 2000000;
                    }
                }

                // This is coming up false and preventing stream copy
                videoStream.IsAVC = null;
            }

            if (isLiveStream)
            {
                mediaSource.AnalyzeDurationMs = 3000;
            }

            // Try to estimate this
            mediaSource.InferTotalBitrate(true);
        }

        public async Task<Tuple<MediaSourceInfo, IDirectStreamProvider>> GetLiveStreamWithDirectStreamProvider(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            var info = await GetLiveStreamInfo(id, cancellationToken).ConfigureAwait(false);
            return new Tuple<MediaSourceInfo, IDirectStreamProvider>(info.MediaSource, info as IDirectStreamProvider);
        }

        private async Task<ILiveStream> GetLiveStreamInfo(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_openStreams.TryGetValue(id, out ILiveStream info))
                {
                    return info;
                }
                else
                {
                    throw new ResourceNotFoundException();
                }
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }
        }

        public async Task<MediaSourceInfo> GetLiveStream(string id, CancellationToken cancellationToken)
        {
            var result = await GetLiveStreamWithDirectStreamProvider(id, cancellationToken).ConfigureAwait(false);
            return result.Item1;
        }

        public async Task CloseLiveStream(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            await _liveStreamSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_openStreams.TryGetValue(id, out ILiveStream liveStream))
                {
                    liveStream.ConsumerCount--;

                    _logger.LogInformation("Live stream {0} consumer count is now {1}", liveStream.OriginalStreamId, liveStream.ConsumerCount);

                    if (liveStream.ConsumerCount <= 0)
                    {
                        _openStreams.Remove(id);

                        _logger.LogInformation("Closing live stream {0}", id);

                        await liveStream.Close().ConfigureAwait(false);
                        _logger.LogInformation("Live stream {0} closed successfully", id);
                    }
                }
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }
        }

        // Do not use a pipe here because Roku http requests to the server will fail, without any explicit error message.
        private const char LiveStreamIdDelimeter = '_';

        private Tuple<IMediaSourceProvider, string> GetProvider(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key");
            }

            var keys = key.Split(new[] { LiveStreamIdDelimeter }, 2);

            var provider = _providers.FirstOrDefault(i => string.Equals(i.GetType().FullName.GetMD5().ToString("N", CultureInfo.InvariantCulture), keys[0], StringComparison.OrdinalIgnoreCase));

            var splitIndex = key.IndexOf(LiveStreamIdDelimeter);
            var keyId = key.Substring(splitIndex + 1);

            return new Tuple<IMediaSourceProvider, string>(provider, keyId);
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
                        var task = CloseLiveStream(key);

                        Task.WaitAll(task);
                    }
                }
            }
        }
    }
}
