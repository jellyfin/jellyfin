#nullable disable

#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
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
        private readonly ILogger<FFProbeProvider> _logger;
        private readonly AudioResolver _audioResolver;
        private readonly SubtitleResolver _subtitleResolver;
        private readonly FFProbeVideoInfo _videoProber;
        private readonly FFProbeAudioInfo _audioProber;
        private readonly Task<ItemUpdateType> _cachedTask = Task.FromResult(ItemUpdateType.None);

        public FFProbeProvider(
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            IItemRepository itemRepo,
            IBlurayExaminer blurayExaminer,
            ILocalizationManager localization,
            IEncodingManager encodingManager,
            IServerConfigurationManager config,
            ISubtitleManager subtitleManager,
            IChapterManager chapterManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            ILoggerFactory loggerFactory,
            NamingOptions namingOptions)
        {
            _logger = loggerFactory.CreateLogger<FFProbeProvider>();
            _audioResolver = new AudioResolver(loggerFactory.CreateLogger<AudioResolver>(), localization, mediaEncoder, fileSystem, namingOptions);
            _subtitleResolver = new SubtitleResolver(loggerFactory.CreateLogger<SubtitleResolver>(), localization, mediaEncoder, fileSystem, namingOptions);
            _videoProber = new FFProbeVideoInfo(
                _logger,
                mediaSourceManager,
                mediaEncoder,
                itemRepo,
                blurayExaminer,
                localization,
                encodingManager,
                config,
                subtitleManager,
                chapterManager,
                libraryManager,
                _audioResolver,
                _subtitleResolver);
            _audioProber = new FFProbeAudioInfo(mediaSourceManager, mediaEncoder, itemRepo, libraryManager);
        }

        public string Name => "ffprobe";

        // Run last
        public int Order => 100;

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
                        _logger.LogDebug("Refreshing {ItemPath} due to date modified timestamp change.", path);
                        return true;
                    }
                }
            }

            if (item.SupportsLocalMetadata && video != null && !video.IsPlaceHolder
                && !video.SubtitleFiles.SequenceEqual(
                    _subtitleResolver.GetExternalFiles(video, directoryService, false)
                    .Select(info => info.Path).ToList(),
                    StringComparer.Ordinal))
            {
                _logger.LogDebug("Refreshing {ItemPath} due to external subtitles change.", item.Path);
                return true;
            }

            if (item.SupportsLocalMetadata && video != null && !video.IsPlaceHolder
                && !video.AudioFiles.SequenceEqual(
                    _audioResolver.GetExternalFiles(video, directoryService, false)
                    .Select(info => info.Path).ToList(),
                    StringComparer.Ordinal))
            {
                _logger.LogDebug("Refreshing {ItemPath} due to external audio change.", item.Path);
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

        public Task<ItemUpdateType> FetchVideoInfo<T>(T item, MetadataRefreshOptions options, CancellationToken cancellationToken)
            where T : Video
        {
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

            return _videoProber.ProbeVideo(item, options, cancellationToken);
        }

        private string NormalizeStrmLine(string line)
        {
            return line.Replace("\t", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        private void FetchShortcutInfo(BaseItem item)
        {
            item.ShortcutPath = File.ReadAllLines(item.Path)
                .Select(NormalizeStrmLine)
                .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i) && !i.StartsWith('#'));
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

            return _audioProber.Probe(item, options, cancellationToken);
        }
    }
}
