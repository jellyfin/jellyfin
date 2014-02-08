using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    public class FFProbeProvider : ICustomMetadataProvider<Episode>,
        ICustomMetadataProvider<MusicVideo>,
        ICustomMetadataProvider<Movie>,
        ICustomMetadataProvider<AdultVideo>,
        ICustomMetadataProvider<LiveTvVideoRecording>,
        ICustomMetadataProvider<LiveTvAudioRecording>,
        ICustomMetadataProvider<Trailer>,
        ICustomMetadataProvider<Video>,
        ICustomMetadataProvider<Audio>,
        IHasChangeMonitor,
        IHasOrder
    {
        private readonly ILogger _logger;
        private readonly IIsoManager _isoManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly IBlurayExaminer _blurayExaminer;
        private readonly ILocalizationManager _localization;

        public string Name
        {
            get { return "ffprobe"; }
        }

        public Task<ItemUpdateType> FetchAsync(Episode item, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(MusicVideo item, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Movie item, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(AdultVideo item, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(LiveTvVideoRecording item, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Trailer item, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Video item, CancellationToken cancellationToken)
        {
            return FetchVideoInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Audio item, CancellationToken cancellationToken)
        {
            return FetchAudioInfo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(LiveTvAudioRecording item, CancellationToken cancellationToken)
        {
            return FetchAudioInfo(item, cancellationToken);
        }

        public FFProbeProvider(ILogger logger, IIsoManager isoManager, IMediaEncoder mediaEncoder, IItemRepository itemRepo, IBlurayExaminer blurayExaminer, ILocalizationManager localization)
        {
            _logger = logger;
            _isoManager = isoManager;
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _blurayExaminer = blurayExaminer;
            _localization = localization;
        }

        private readonly Task<ItemUpdateType> _cachedTask = Task.FromResult(ItemUpdateType.Unspecified);
        public Task<ItemUpdateType> FetchVideoInfo<T>(T item, CancellationToken cancellationToken)
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

            var prober = new FFProbeVideoInfo(_logger, _isoManager, _mediaEncoder, _itemRepo, _blurayExaminer, _localization);

            return prober.ProbeVideo(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAudioInfo<T>(T item, CancellationToken cancellationToken)
            where T : Audio
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return _cachedTask;
            }

            var prober = new FFProbeAudioInfo(_mediaEncoder, _itemRepo);

            return prober.Probe(item, cancellationToken);
        }

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            return item.DateModified > date;
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
