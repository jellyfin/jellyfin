using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.MediaInfo
{
    public class FFProbeProvider : ICustomMetadataProvider<Episode>,
        ICustomMetadataProvider<MusicVideo>,
        ICustomMetadataProvider<Movie>,
        ICustomMetadataProvider<LiveTvVideoRecording>,
        ICustomMetadataProvider<LiveTvAudioRecording>,
        ICustomMetadataProvider<Trailer>,
        ICustomMetadataProvider<Video>,
        ICustomMetadataProvider<Audio>,
        IHasItemChangeMonitor,
        IHasOrder,
        IForcedProvider,
        IPreRefreshProvider
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
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IChapterManager _chapterManager;
        private readonly ILibraryManager _libraryManager;

        public string Name
        {
            get { return "ffprobe"; }
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

        public Task<ItemUpdateType> FetchAsync(LiveTvVideoRecording item, MetadataRefreshOptions options, CancellationToken cancellationToken)
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
            return FetchAudioInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(LiveTvAudioRecording item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAudioInfo(item, cancellationToken);
        }

        public FFProbeProvider(ILogger logger, IIsoManager isoManager, IMediaEncoder mediaEncoder, IItemRepository itemRepo, IBlurayExaminer blurayExaminer, ILocalizationManager localization, IApplicationPaths appPaths, IJsonSerializer json, IEncodingManager encodingManager, IFileSystem fileSystem, IServerConfigurationManager config, ISubtitleManager subtitleManager, IChapterManager chapterManager, ILibraryManager libraryManager)
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
            _fileSystem = fileSystem;
            _config = config;
            _subtitleManager = subtitleManager;
            _chapterManager = chapterManager;
            _libraryManager = libraryManager;
        }

        private readonly Task<ItemUpdateType> _cachedTask = Task.FromResult(ItemUpdateType.None);
        public Task<ItemUpdateType> FetchVideoInfo<T>(T item, MetadataRefreshOptions options, CancellationToken cancellationToken)
            where T : Video
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return _cachedTask;
            }

            if (item.VideoType == VideoType.Iso && !_isoManager.CanMount(item.Path))
            {
                return _cachedTask;
            }

            if (item.VideoType == VideoType.HdDvd)
            {
                return _cachedTask;
            }

            if (item.IsPlaceHolder)
            {
                return _cachedTask;
            }

            if (item.IsShortcut)
            {
                FetchShortcutInfo(item);
                return Task.FromResult(ItemUpdateType.MetadataImport);
            }

            var prober = new FFProbeVideoInfo(_logger, _isoManager, _mediaEncoder, _itemRepo, _blurayExaminer, _localization, _appPaths, _json, _encodingManager, _fileSystem, _config, _subtitleManager, _chapterManager, _libraryManager);

            return prober.ProbeVideo(item, options, cancellationToken);
        }

        private void FetchShortcutInfo(Video video)
        {
			video.ShortcutPath = _fileSystem.ReadAllText(video.Path);
        }

        public Task<ItemUpdateType> FetchAudioInfo<T>(T item, CancellationToken cancellationToken)
            where T : Audio
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return _cachedTask;
            }

            var prober = new FFProbeAudioInfo(_mediaEncoder, _itemRepo, _appPaths, _json, _libraryManager);

            return prober.Probe(item, cancellationToken);
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            var file = directoryService.GetFile(item.Path);
            if (file != null && file.LastWriteTimeUtc != item.DateModified)
            {
                return true;
            }

            if (item.SupportsLocalMetadata)
            {
                var video = item as Video;

                if (video != null && !video.IsPlaceHolder)
                {
                    return !video.SubtitleFiles
                        .SequenceEqual(SubtitleResolver.GetSubtitleFiles(video, directoryService, _fileSystem, false)
                        .Select(i => i.FullName)
                        .OrderBy(i => i), StringComparer.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        public int Order
        {
            get
            {
                // Run last
                return 100;
            }
        }
    }
}
