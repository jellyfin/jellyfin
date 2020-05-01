using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    public class FFProbeProvider : ICustomMetadataProvider<Episode>,
        ICustomMetadataProvider<MusicVideo>,
        ICustomMetadataProvider<Movie>,
        ICustomMetadataProvider<Trailer>,
        ICustomMetadataProvider<Video>,
        ICustomMetadataProvider<Audio>,
        ICustomMetadataProvider<AudioBook>,
        IHasOrder,
        IForcedProvider,
        IPreRefreshProvider,
        IHasItemChangeMonitor
    {
        private readonly ILogger _logger;
        private readonly IIsoManager _isoManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly IBlurayExaminer _blurayExaminer;
        private readonly ILocalizationManager _localization;
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;
        private readonly IEncodingManager _encodingManager;
        private readonly IServerConfigurationManager _config;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IChapterManager _chapterManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IChannelManager _channelManager;
        private readonly IMediaSourceManager _mediaSourceManager;

        public string Name => "ffprobe";

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var video = item as Video;
            if (video == null || video.VideoType == VideoType.VideoFile || video.VideoType == VideoType.Iso)
            {
                var path = item.Path;

                if (!string.IsNullOrWhiteSpace(path) && item.IsFileProtocol)
                {
                    var file = directoryService.GetFile(path);
                    if (file != null && file.LastWriteTimeUtc != item.DateModified)
                    {
                        _logger.LogDebug("Refreshing {0} due to date modified timestamp change.", path);
                        return true;
                    }
                }
            }

            if (item.SupportsLocalMetadata && video != null && !video.IsPlaceHolder
                && !video.SubtitleFiles.SequenceEqual(
                        _subtitleResolver.GetExternalSubtitleFiles(video, directoryService, false), StringComparer.Ordinal))
            {
                _logger.LogDebug("Refreshing {0} due to external subtitles change.", item.Path);
                return true;
            }

            return false;
        }

        public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(MusicVideo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Trailer item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Audio item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAudioInfo(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(AudioBook item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAudioInfo(item, options, cancellationToken);
        }

        private SubtitleResolver _subtitleResolver;

        public FFProbeProvider(
            ILogger<FFProbeProvider> logger,
            IMediaSourceManager mediaSourceManager,
            IChannelManager channelManager,
            IIsoManager isoManager,
            IMediaEncoder mediaEncoder,
            IItemRepository itemRepo,
            IBlurayExaminer blurayExaminer,
            ILocalizationManager localization,
            IApplicationPaths appPaths,
            IJsonSerializer json,
            IEncodingManager encodingManager,
            IServerConfigurationManager config,
            ISubtitleManager subtitleManager,
            IChapterManager chapterManager,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _isoManager = isoManager;
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _blurayExaminer = blurayExaminer;
            _localization = localization;
            _appPaths = appPaths;
            _json = json;
            _encodingManager = encodingManager;
            _config = config;
            _subtitleManager = subtitleManager;
            _chapterManager = chapterManager;
            _libraryManager = libraryManager;
            _channelManager = channelManager;
            _mediaSourceManager = mediaSourceManager;

            _subtitleResolver = new SubtitleResolver(BaseItem.LocalizationManager);
        }

        private readonly Task<ItemUpdateType> _cachedTask = Task.FromResult(ItemUpdateType.None);
        public Task<ItemUpdateType> FetchVideoInfo<T>(T item, MetadataRefreshOptions options, CancellationToken cancellationToken)
            where T : Video
        {
            if (item.VideoType == VideoType.Iso)
            {
                return _cachedTask;
            }

            if (item.IsPlaceHolder)
            {
                return _cachedTask;
            }

            if (!item.IsCompleteMedia)
            {
                return _cachedTask;
            }

            if (item.IsVirtualItem)
            {
                return _cachedTask;
            }

            if (!options.EnableRemoteContentProbe && !item.IsFileProtocol)
            {
                return _cachedTask;
            }

            if (item.IsShortcut)
            {
                FetchShortcutInfo(item);
            }

            var prober = new FFProbeVideoInfo(
                _logger,
                _mediaSourceManager,
                _mediaEncoder,
                _itemRepo,
                _blurayExaminer,
                _localization,
                _encodingManager,
                _config,
                _subtitleManager,
                _chapterManager,
                _libraryManager);

            return prober.ProbeVideo(item, options, cancellationToken);
        }

        private string NormalizeStrmLine(string line)
        {
            return line.Replace("\t", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();
        }

        private void FetchShortcutInfo(BaseItem item)
        {
            item.ShortcutPath = File.ReadAllLines(item.Path)
                .Select(NormalizeStrmLine)
                .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i) && !i.StartsWith("#", StringComparison.OrdinalIgnoreCase));
        }

        public Task<ItemUpdateType> FetchAudioInfo<T>(T item, MetadataRefreshOptions options, CancellationToken cancellationToken)
            where T : Audio
        {
            if (item.IsVirtualItem)
            {
                return _cachedTask;
            }

            if (!options.EnableRemoteContentProbe && !item.IsFileProtocol)
            {
                return _cachedTask;
            }

            if (item.IsShortcut)
            {
                FetchShortcutInfo(item);
            }

            var prober = new FFProbeAudioInfo(_mediaSourceManager, _mediaEncoder, _itemRepo, _appPaths, _json, _libraryManager);

            return prober.Probe(item, options, cancellationToken);
        }
        // Run last
        public int Order => 100;
    }
}
