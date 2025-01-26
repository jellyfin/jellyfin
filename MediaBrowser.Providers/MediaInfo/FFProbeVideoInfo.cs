#pragma warning disable CA1068, CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    public class FFProbeVideoInfo
    {
        private readonly ILogger<FFProbeVideoInfo> _logger;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly IBlurayExaminer _blurayExaminer;
        private readonly ILocalizationManager _localization;
        private readonly IEncodingManager _encodingManager;
        private readonly IServerConfigurationManager _config;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IChapterRepository _chapterManager;
        private readonly ILibraryManager _libraryManager;
        private readonly AudioResolver _audioResolver;
        private readonly SubtitleResolver _subtitleResolver;
        private readonly IMediaAttachmentRepository _mediaAttachmentRepository;
        private readonly IMediaStreamRepository _mediaStreamRepository;

        public FFProbeVideoInfo(
            ILogger<FFProbeVideoInfo> logger,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            IItemRepository itemRepo,
            IBlurayExaminer blurayExaminer,
            ILocalizationManager localization,
            IEncodingManager encodingManager,
            IServerConfigurationManager config,
            ISubtitleManager subtitleManager,
            IChapterRepository chapterManager,
            ILibraryManager libraryManager,
            AudioResolver audioResolver,
            SubtitleResolver subtitleResolver,
            IMediaAttachmentRepository mediaAttachmentRepository,
            IMediaStreamRepository mediaStreamRepository)
        {
            _logger = logger;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _blurayExaminer = blurayExaminer;
            _localization = localization;
            _encodingManager = encodingManager;
            _config = config;
            _subtitleManager = subtitleManager;
            _chapterManager = chapterManager;
            _libraryManager = libraryManager;
            _audioResolver = audioResolver;
            _subtitleResolver = subtitleResolver;
            _mediaAttachmentRepository = mediaAttachmentRepository;
            _mediaStreamRepository = mediaStreamRepository;
            _mediaStreamRepository = mediaStreamRepository;
        }

        public async Task<ItemUpdateType> ProbeVideo<T>(
            T item,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
            where T : Video
        {
            BlurayDiscInfo? blurayDiscInfo = null;

            Model.MediaInfo.MediaInfo? mediaInfoResult = null;

            if (!item.IsShortcut || options.EnableRemoteContentProbe)
            {
                if (item.VideoType == VideoType.Dvd)
                {
                    // Get list of playable .vob files
                    var vobs = _mediaEncoder.GetPrimaryPlaylistVobFiles(item.Path, null);

                    // Return if no playable .vob files are found
                    if (vobs.Count == 0)
                    {
                        _logger.LogError("No playable .vob files found in DVD structure, skipping FFprobe.");
                        return ItemUpdateType.MetadataImport;
                    }

                    // Fetch metadata of first .vob file
                    mediaInfoResult = await GetMediaInfo(
                        new Video
                        {
                            Path = vobs[0]
                        },
                        cancellationToken).ConfigureAwait(false);

                    // Sum up the runtime of all .vob files skipping the first .vob
                    for (var i = 1; i < vobs.Count; i++)
                    {
                        var tmpMediaInfo = await GetMediaInfo(
                            new Video
                            {
                                Path = vobs[i]
                            },
                            cancellationToken).ConfigureAwait(false);

                        mediaInfoResult.RunTimeTicks += tmpMediaInfo.RunTimeTicks;
                    }
                }
                else if (item.VideoType == VideoType.BluRay)
                {
                    // Get BD disc information
                    blurayDiscInfo = GetBDInfo(item.Path);

                    // Return if no playable .m2ts files are found
                    if (blurayDiscInfo is null || blurayDiscInfo.Files.Length == 0)
                    {
                        _logger.LogError("No playable .m2ts files found in Blu-ray structure, skipping FFprobe.");
                        return ItemUpdateType.MetadataImport;
                    }

                    // Fetch metadata of first .m2ts file
                    mediaInfoResult = await GetMediaInfo(
                        new Video
                        {
                            Path = blurayDiscInfo.Files[0]
                        },
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    mediaInfoResult = await GetMediaInfo(item, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            await Fetch(item, cancellationToken, mediaInfoResult, blurayDiscInfo, options).ConfigureAwait(false);

            return ItemUpdateType.MetadataImport;
        }

        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(
            Video item,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = item.Path;
            var protocol = item.PathProtocol ?? MediaProtocol.File;

            if (item.IsShortcut)
            {
                path = item.ShortcutPath;
                protocol = _mediaSourceManager.GetPathProtocol(path);
            }

            return _mediaEncoder.GetMediaInfo(
                new MediaInfoRequest
                {
                    ExtractChapters = true,
                    MediaType = DlnaProfileType.Video,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = protocol,
                        VideoType = item.VideoType,
                        IsoType = item.IsoType
                    }
                },
                cancellationToken);
        }

        protected async Task Fetch(
            Video video,
            CancellationToken cancellationToken,
            Model.MediaInfo.MediaInfo? mediaInfo,
            BlurayDiscInfo? blurayInfo,
            MetadataRefreshOptions options)
        {
            List<MediaStream> mediaStreams = new List<MediaStream>();
            IReadOnlyList<MediaAttachment> mediaAttachments;
            ChapterInfo[] chapters;

            // Add external streams before adding the streams from the file to preserve stream IDs on remote videos
            await AddExternalSubtitlesAsync(video, mediaStreams, options, cancellationToken).ConfigureAwait(false);

            await AddExternalAudioAsync(video, mediaStreams, options, cancellationToken).ConfigureAwait(false);

            var startIndex = mediaStreams.Count == 0 ? 0 : (mediaStreams.Max(i => i.Index) + 1);

            if (mediaInfo is not null)
            {
                foreach (var mediaStream in mediaInfo.MediaStreams)
                {
                    mediaStream.Index = startIndex++;
                    mediaStreams.Add(mediaStream);
                }

                mediaAttachments = mediaInfo.MediaAttachments;
                video.TotalBitrate = mediaInfo.Bitrate;
                video.RunTimeTicks = mediaInfo.RunTimeTicks;
                video.Size = mediaInfo.Size;
                video.Container = mediaInfo.Container;

                chapters = mediaInfo.Chapters ?? Array.Empty<ChapterInfo>();
                if (blurayInfo is not null)
                {
                    FetchBdInfo(video, ref chapters, mediaStreams, blurayInfo);
                }
            }
            else
            {
                foreach (var mediaStream in video.GetMediaStreams())
                {
                    if (!mediaStream.IsExternal)
                    {
                        mediaStream.Index = startIndex++;
                        mediaStreams.Add(mediaStream);
                    }
                }

                mediaAttachments = Array.Empty<MediaAttachment>();
                chapters = Array.Empty<ChapterInfo>();
            }

            var libraryOptions = _libraryManager.GetLibraryOptions(video);

            if (mediaInfo is not null)
            {
                FetchEmbeddedInfo(video, mediaInfo, options, libraryOptions);
                FetchPeople(video, mediaInfo, options);
                video.Timestamp = mediaInfo.Timestamp;
                video.Video3DFormat ??= mediaInfo.Video3DFormat;
            }

            if (libraryOptions.AllowEmbeddedSubtitles == EmbeddedSubtitleOptions.AllowText || libraryOptions.AllowEmbeddedSubtitles == EmbeddedSubtitleOptions.AllowNone)
            {
                _logger.LogDebug("Disabling embedded image subtitles for {Path} due to DisableEmbeddedImageSubtitles setting", video.Path);
                mediaStreams.RemoveAll(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal && !i.IsTextSubtitleStream);
            }

            if (libraryOptions.AllowEmbeddedSubtitles == EmbeddedSubtitleOptions.AllowImage || libraryOptions.AllowEmbeddedSubtitles == EmbeddedSubtitleOptions.AllowNone)
            {
                _logger.LogDebug("Disabling embedded text subtitles for {Path} due to DisableEmbeddedTextSubtitles setting", video.Path);
                mediaStreams.RemoveAll(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal && i.IsTextSubtitleStream);
            }

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            video.Height = videoStream?.Height ?? 0;
            video.Width = videoStream?.Width ?? 0;

            video.DefaultVideoStreamIndex = videoStream?.Index;

            video.HasSubtitles = mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle);

            _mediaStreamRepository.SaveMediaStreams(video.Id, mediaStreams, cancellationToken);

            if (mediaAttachments.Any())
            {
                _mediaAttachmentRepository.SaveMediaAttachments(video.Id, mediaAttachments, cancellationToken);
            }

            if (options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh
                || options.MetadataRefreshMode == MetadataRefreshMode.Default)
            {
                if (_config.Configuration.DummyChapterDuration > 0 && chapters.Length == 0 && mediaStreams.Any(i => i.Type == MediaStreamType.Video))
                {
                    chapters = CreateDummyChapters(video);
                }

                NormalizeChapterNames(chapters);

                var extractDuringScan = false;
                if (libraryOptions is not null)
                {
                    extractDuringScan = libraryOptions.ExtractChapterImagesDuringLibraryScan;
                }

                await _encodingManager.RefreshChapterImages(video, options.DirectoryService, chapters, extractDuringScan, false, cancellationToken).ConfigureAwait(false);

                _chapterManager.SaveChapters(video.Id, chapters);
            }
        }

        private void NormalizeChapterNames(ChapterInfo[] chapters)
        {
            for (int i = 0; i < chapters.Length; i++)
            {
                string? name = chapters[i].Name;
                // Check if the name is empty and/or if the name is a time
                // Some ripping programs do that.
                if (string.IsNullOrWhiteSpace(name)
                    || TimeSpan.TryParse(name, out _))
                {
                    chapters[i].Name = string.Format(
                        CultureInfo.InvariantCulture,
                        _localization.GetLocalizedString("ChapterNameValue"),
                        (i + 1).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private void FetchBdInfo(Video video, ref ChapterInfo[] chapters, List<MediaStream> mediaStreams, BlurayDiscInfo blurayInfo)
        {
            if (blurayInfo.Files.Length <= 1)
            {
                return;
            }

            var ffmpegVideoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

            // Fill video properties from the BDInfo result
            mediaStreams.Clear();
            mediaStreams.AddRange(blurayInfo.MediaStreams);

            if (blurayInfo.RunTimeTicks.HasValue && blurayInfo.RunTimeTicks.Value > 0)
            {
                video.RunTimeTicks = blurayInfo.RunTimeTicks;
            }

            if (blurayInfo.Chapters is not null)
            {
                double[] brChapter = blurayInfo.Chapters;
                chapters = new ChapterInfo[brChapter.Length];
                for (int i = 0; i < brChapter.Length; i++)
                {
                    chapters[i] = new ChapterInfo
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(brChapter[i]).Ticks
                    };
                }
            }

            var blurayVideoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

            // Use the ffprobe values if these are empty
            if (blurayVideoStream is not null && ffmpegVideoStream is not null)
            {
                // Always use ffmpeg's detected codec since that is what the rest of the codebase expects.
                blurayVideoStream.Codec = ffmpegVideoStream.Codec;
                blurayVideoStream.BitRate = blurayVideoStream.BitRate.GetValueOrDefault() == 0 ? ffmpegVideoStream.BitRate : blurayVideoStream.BitRate;
                blurayVideoStream.Width = blurayVideoStream.Width.GetValueOrDefault() == 0 ? ffmpegVideoStream.Width : blurayVideoStream.Width;
                blurayVideoStream.Height = blurayVideoStream.Height.GetValueOrDefault() == 0 ? ffmpegVideoStream.Width : blurayVideoStream.Height;
                blurayVideoStream.ColorRange = ffmpegVideoStream.ColorRange;
                blurayVideoStream.ColorSpace = ffmpegVideoStream.ColorSpace;
                blurayVideoStream.ColorTransfer = ffmpegVideoStream.ColorTransfer;
                blurayVideoStream.ColorPrimaries = ffmpegVideoStream.ColorPrimaries;
            }
        }

        /// <summary>
        /// Gets information about the longest playlist on a bdrom.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoStream.</returns>
        private BlurayDiscInfo? GetBDInfo(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            try
            {
                return _blurayExaminer.GetDiscInfo(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting BDInfo");
                return null;
            }
        }

        private void FetchEmbeddedInfo(Video video, Model.MediaInfo.MediaInfo data, MetadataRefreshOptions refreshOptions, LibraryOptions libraryOptions)
        {
            var replaceData = refreshOptions.ReplaceAllMetadata;

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataField.OfficialRating))
            {
                if (string.IsNullOrWhiteSpace(video.OfficialRating) || replaceData)
                {
                    video.OfficialRating = data.OfficialRating;
                }
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataField.Genres))
            {
                if (video.Genres.Length == 0 || replaceData)
                {
                    video.Genres = Array.Empty<string>();

                    foreach (var genre in data.Genres)
                    {
                        video.AddGenre(genre);
                    }
                }
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataField.Studios))
            {
                if (video.Studios.Length == 0 || replaceData)
                {
                    video.SetStudios(data.Studios);
                }
            }

            if (!video.IsLocked && video is MusicVideo musicVideo)
            {
                if (string.IsNullOrEmpty(musicVideo.Album) || replaceData)
                {
                    musicVideo.Album = data.Album;
                }

                if (musicVideo.Artists.Count == 0 || replaceData)
                {
                    musicVideo.Artists = data.Artists;
                }
            }

            if (data.ProductionYear.HasValue)
            {
                if (!video.ProductionYear.HasValue || replaceData)
                {
                    video.ProductionYear = data.ProductionYear;
                }
            }

            if (data.PremiereDate.HasValue)
            {
                if (!video.PremiereDate.HasValue || replaceData)
                {
                    video.PremiereDate = data.PremiereDate;
                }
            }

            if (data.IndexNumber.HasValue)
            {
                if (!video.IndexNumber.HasValue || replaceData)
                {
                    video.IndexNumber = data.IndexNumber;
                }
            }

            if (data.ParentIndexNumber.HasValue)
            {
                if (!video.ParentIndexNumber.HasValue || replaceData)
                {
                    video.ParentIndexNumber = data.ParentIndexNumber;
                }
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataField.Name))
            {
                if (!string.IsNullOrWhiteSpace(data.Name) && libraryOptions.EnableEmbeddedTitles)
                {
                    // Separate option to use the embedded name for extras because it will often be the same name as the movie
                    if (!video.ExtraType.HasValue || libraryOptions.EnableEmbeddedExtrasTitles)
                    {
                        video.Name = data.Name;
                    }
                }

                if (!string.IsNullOrWhiteSpace(data.ForcedSortName))
                {
                    video.ForcedSortName = data.ForcedSortName;
                }
            }

            // If we don't have a ProductionYear try and get it from PremiereDate
            if (video.PremiereDate.HasValue && !video.ProductionYear.HasValue)
            {
                video.ProductionYear = video.PremiereDate.Value.ToLocalTime().Year;
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataField.Overview))
            {
                if (string.IsNullOrWhiteSpace(video.Overview) || replaceData)
                {
                    video.Overview = data.Overview;
                }
            }
        }

        private void FetchPeople(Video video, Model.MediaInfo.MediaInfo data, MetadataRefreshOptions options)
        {
            if (video.IsLocked
                || video.LockedFields.Contains(MetadataField.Cast)
                || data.People.Length == 0)
            {
                return;
            }

            if (options.ReplaceAllMetadata || _libraryManager.GetPeople(video).Count == 0)
            {
                var people = new List<PersonInfo>();

                foreach (var person in data.People)
                {
                    PeopleHelper.AddPerson(people, new PersonInfo
                    {
                        Name = person.Name,
                        Type = person.Type,
                        Role = person.Role
                    });
                }

                _libraryManager.UpdatePeople(video, people);
            }
        }

        /// <summary>
        /// Adds the external subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="currentStreams">The current streams.</param>
        /// <param name="options">The refreshOptions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task AddExternalSubtitlesAsync(
            Video video,
            List<MediaStream> currentStreams,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
        {
            var startIndex = currentStreams.Count == 0 ? 0 : (currentStreams.Select(i => i.Index).Max() + 1);
            var externalSubtitleStreams = await _subtitleResolver.GetExternalStreamsAsync(video, startIndex, options.DirectoryService, false, cancellationToken).ConfigureAwait(false);

            var enableSubtitleDownloading = options.MetadataRefreshMode == MetadataRefreshMode.Default ||
                                            options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh;

            var subtitleOptions = _config.GetConfiguration<SubtitleOptions>("subtitles");

            var libraryOptions = _libraryManager.GetLibraryOptions(video);

            string[] subtitleDownloadLanguages;
            bool skipIfEmbeddedSubtitlesPresent;
            bool skipIfAudioTrackMatches;
            bool requirePerfectMatch;
            bool enabled;

            if (libraryOptions.SubtitleDownloadLanguages is null)
            {
                subtitleDownloadLanguages = subtitleOptions.DownloadLanguages;
                skipIfEmbeddedSubtitlesPresent = subtitleOptions.SkipIfEmbeddedSubtitlesPresent;
                skipIfAudioTrackMatches = subtitleOptions.SkipIfAudioTrackMatches;
                requirePerfectMatch = subtitleOptions.RequirePerfectMatch;
                enabled = (subtitleOptions.DownloadEpisodeSubtitles &&
                video is Episode) ||
                (subtitleOptions.DownloadMovieSubtitles &&
                video is Movie);
            }
            else
            {
                subtitleDownloadLanguages = libraryOptions.SubtitleDownloadLanguages;
                skipIfEmbeddedSubtitlesPresent = libraryOptions.SkipSubtitlesIfEmbeddedSubtitlesPresent;
                skipIfAudioTrackMatches = libraryOptions.SkipSubtitlesIfAudioTrackMatches;
                requirePerfectMatch = libraryOptions.RequirePerfectSubtitleMatch;
                enabled = true;
            }

            if (enableSubtitleDownloading && enabled)
            {
                var downloadedLanguages = await new SubtitleDownloader(
                    _logger,
                    _subtitleManager).DownloadSubtitles(
                        video,
                        currentStreams.Concat(externalSubtitleStreams).ToList(),
                        skipIfEmbeddedSubtitlesPresent,
                        skipIfAudioTrackMatches,
                        requirePerfectMatch,
                        subtitleDownloadLanguages,
                        libraryOptions.DisabledSubtitleFetchers,
                        libraryOptions.SubtitleFetcherOrder,
                        true,
                        cancellationToken).ConfigureAwait(false);

                // Rescan
                if (downloadedLanguages.Count > 0)
                {
                    externalSubtitleStreams = await _subtitleResolver.GetExternalStreamsAsync(video, startIndex, options.DirectoryService, true, cancellationToken).ConfigureAwait(false);
                }
            }

            video.SubtitleFiles = externalSubtitleStreams.Select(i => i.Path).Distinct().ToArray();

            currentStreams.AddRange(externalSubtitleStreams);
        }

        /// <summary>
        /// Adds the external audio.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="currentStreams">The current streams.</param>
        /// <param name="options">The refreshOptions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task AddExternalAudioAsync(
            Video video,
            List<MediaStream> currentStreams,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
        {
            var startIndex = currentStreams.Count == 0 ? 0 : currentStreams.Max(i => i.Index) + 1;
            var externalAudioStreams = await _audioResolver.GetExternalStreamsAsync(video, startIndex, options.DirectoryService, false, cancellationToken).ConfigureAwait(false);

            video.AudioFiles = externalAudioStreams.Select(i => i.Path).Distinct().ToArray();

            currentStreams.AddRange(externalAudioStreams);
        }

        /// <summary>
        /// Creates dummy chapters.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <returns>An array of dummy chapters.</returns>
        internal ChapterInfo[] CreateDummyChapters(Video video)
        {
            var runtime = video.RunTimeTicks.GetValueOrDefault();

            // Only process files with a runtime greater than 0 and less than 12h. The latter are likely corrupted.
            if (runtime < 0 || runtime > TimeSpan.FromHours(12).Ticks)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} has an invalid runtime of {1} minutes",
                        video.Name,
                        TimeSpan.FromTicks(runtime).TotalMinutes));
            }

            long dummyChapterDuration = TimeSpan.FromSeconds(_config.Configuration.DummyChapterDuration).Ticks;
            if (runtime <= dummyChapterDuration)
            {
                return Array.Empty<ChapterInfo>();
            }

            int chapterCount = (int)(runtime / dummyChapterDuration);
            var chapters = new ChapterInfo[chapterCount];

            long currentChapterTicks = 0;
            for (int i = 0; i < chapterCount; i++)
            {
                chapters[i] = new ChapterInfo
                {
                    StartPositionTicks = currentChapterTicks
                };

                currentChapterTicks += dummyChapterDuration;
            }

            return chapters;
        }
    }
}
