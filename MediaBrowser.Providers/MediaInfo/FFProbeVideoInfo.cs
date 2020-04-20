#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DvdLib.Ifo;
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
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    public class FFProbeVideoInfo
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly IBlurayExaminer _blurayExaminer;
        private readonly ILocalizationManager _localization;
        private readonly IEncodingManager _encodingManager;
        private readonly IServerConfigurationManager _config;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IChapterManager _chapterManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;

        private readonly long _dummyChapterDuration = TimeSpan.FromMinutes(5).Ticks;

        public FFProbeVideoInfo(
            ILogger logger,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            IItemRepository itemRepo,
            IBlurayExaminer blurayExaminer,
            ILocalizationManager localization,
            IEncodingManager encodingManager,
            IServerConfigurationManager config,
            ISubtitleManager subtitleManager,
            IChapterManager chapterManager,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _blurayExaminer = blurayExaminer;
            _localization = localization;
            _encodingManager = encodingManager;
            _config = config;
            _subtitleManager = subtitleManager;
            _chapterManager = chapterManager;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
        }

        public async Task<ItemUpdateType> ProbeVideo<T>(
            T item,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
            where T : Video
        {
            BlurayDiscInfo blurayDiscInfo = null;

            Model.MediaInfo.MediaInfo mediaInfoResult = null;

            if (!item.IsShortcut || options.EnableRemoteContentProbe)
            {
                string[] streamFileNames = null;

                if (item.VideoType == VideoType.Dvd)
                {
                    streamFileNames = FetchFromDvdLib(item);

                    if (streamFileNames.Length == 0)
                    {
                        _logger.LogError("No playable vobs found in dvd structure, skipping ffprobe.");
                        return ItemUpdateType.MetadataImport;
                    }
                }
                else if (item.VideoType == VideoType.BluRay)
                {
                    var inputPath = item.Path;

                    blurayDiscInfo = GetBDInfo(inputPath);

                    streamFileNames = blurayDiscInfo.Files;

                    if (streamFileNames.Length == 0)
                    {
                        _logger.LogError("No playable vobs found in bluray structure, skipping ffprobe.");
                        return ItemUpdateType.MetadataImport;
                    }
                }

                if (streamFileNames == null)
                {
                    streamFileNames = Array.Empty<string>();
                }

                mediaInfoResult = await GetMediaInfo(item, streamFileNames, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
            }

            await Fetch(item, cancellationToken, mediaInfoResult, blurayDiscInfo, options).ConfigureAwait(false);

            return ItemUpdateType.MetadataImport;
        }

        private Task<Model.MediaInfo.MediaInfo> GetMediaInfo(
            Video item,
            string[] streamFileNames,
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
                    PlayableStreamFileNames = streamFileNames,
                    ExtractChapters = true,
                    MediaType = DlnaProfileType.Video,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = protocol,
                        VideoType = item.VideoType
                    }
                },
                cancellationToken);
        }

        protected async Task Fetch(
            Video video,
            CancellationToken cancellationToken,
            Model.MediaInfo.MediaInfo mediaInfo,
            BlurayDiscInfo blurayInfo,
            MetadataRefreshOptions options)
        {
            List<MediaStream> mediaStreams;
            IReadOnlyList<MediaAttachment> mediaAttachments;
            ChapterInfo[] chapters;

            if (mediaInfo != null)
            {
                mediaStreams = mediaInfo.MediaStreams;
                mediaAttachments = mediaInfo.MediaAttachments;

                video.TotalBitrate = mediaInfo.Bitrate;
                //video.FormatName = (mediaInfo.Container ?? string.Empty)
                //    .Replace("matroska", "mkv", StringComparison.OrdinalIgnoreCase);

                // For dvd's this may not always be accurate, so don't set the runtime if the item already has one
                var needToSetRuntime = video.VideoType != VideoType.Dvd || video.RunTimeTicks == null || video.RunTimeTicks.Value == 0;

                if (needToSetRuntime)
                {
                    video.RunTimeTicks = mediaInfo.RunTimeTicks;
                }

                video.Size = mediaInfo.Size;

                if (video.VideoType == VideoType.VideoFile)
                {
                    var extension = (Path.GetExtension(video.Path) ?? string.Empty).TrimStart('.');

                    video.Container = extension;
                }
                else
                {
                    video.Container = null;
                }

                video.Container = mediaInfo.Container;

                chapters = mediaInfo.Chapters == null ? Array.Empty<ChapterInfo>() : mediaInfo.Chapters;
                if (blurayInfo != null)
                {
                    FetchBdInfo(video, ref chapters, mediaStreams, blurayInfo);
                }
            }
            else
            {
                mediaStreams = new List<MediaStream>();
                mediaAttachments = Array.Empty<MediaAttachment>();
                chapters = Array.Empty<ChapterInfo>();
            }

            await AddExternalSubtitles(video, mediaStreams, options, cancellationToken).ConfigureAwait(false);

            var libraryOptions = _libraryManager.GetLibraryOptions(video);

            if (mediaInfo != null)
            {
                FetchEmbeddedInfo(video, mediaInfo, options, libraryOptions);
                FetchPeople(video, mediaInfo, options);
                video.Timestamp = mediaInfo.Timestamp;
                video.Video3DFormat ??= mediaInfo.Video3DFormat;
            }

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            video.Height = videoStream?.Height ?? 0;
            video.Width = videoStream?.Width ?? 0;

            video.DefaultVideoStreamIndex = videoStream == null ? (int?)null : videoStream.Index;

            video.HasSubtitles = mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle);

            _itemRepo.SaveMediaStreams(video.Id, mediaStreams, cancellationToken);
            _itemRepo.SaveMediaAttachments(video.Id, mediaAttachments, cancellationToken);

            if (options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh ||
                options.MetadataRefreshMode == MetadataRefreshMode.Default)
            {
                if (chapters.Length == 0 && mediaStreams.Any(i => i.Type == MediaStreamType.Video))
                {
                    chapters = CreateDummyChapters(video);
                }

                NormalizeChapterNames(chapters);

                var extractDuringScan = false;
                if (libraryOptions != null)
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
                string name = chapters[i].Name;
                // Check if the name is empty and/or if the name is a time
                // Some ripping programs do that.
                if (string.IsNullOrWhiteSpace(name) ||
                    TimeSpan.TryParse(name, out _))
                {
                    chapters[i].Name = string.Format(
                        CultureInfo.InvariantCulture,
                        _localization.GetLocalizedString("ChapterNameValue"),
                        (i + 1).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private void FetchBdInfo(BaseItem item, ref ChapterInfo[] chapters, List<MediaStream> mediaStreams, BlurayDiscInfo blurayInfo)
        {
            var video = (Video)item;

            //video.PlayableStreamFileNames = blurayInfo.Files.ToList();

            // Use BD Info if it has multiple m2ts. Otherwise, treat it like a video file and rely more on ffprobe output
            if (blurayInfo.Files.Length > 1)
            {
                int? currentHeight = null;
                int? currentWidth = null;
                int? currentBitRate = null;

                var videoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

                // Grab the values that ffprobe recorded
                if (videoStream != null)
                {
                    currentBitRate = videoStream.BitRate;
                    currentWidth = videoStream.Width;
                    currentHeight = videoStream.Height;
                }

                // Fill video properties from the BDInfo result
                mediaStreams.Clear();
                mediaStreams.AddRange(blurayInfo.MediaStreams);

                if (blurayInfo.RunTimeTicks.HasValue && blurayInfo.RunTimeTicks.Value > 0)
                {
                    video.RunTimeTicks = blurayInfo.RunTimeTicks;
                }

                if (blurayInfo.Chapters != null)
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

                videoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

                // Use the ffprobe values if these are empty
                if (videoStream != null)
                {
                    videoStream.BitRate = IsEmpty(videoStream.BitRate) ? currentBitRate : videoStream.BitRate;
                    videoStream.Width = IsEmpty(videoStream.Width) ? currentWidth : videoStream.Width;
                    videoStream.Height = IsEmpty(videoStream.Height) ? currentHeight : videoStream.Height;
                }
            }
        }

        private bool IsEmpty(int? num)
        {
            return !num.HasValue || num.Value == 0;
        }

        /// <summary>
        /// Gets information about the longest playlist on a bdrom
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoStream.</returns>
        private BlurayDiscInfo GetBDInfo(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

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
            var isFullRefresh = refreshOptions.MetadataRefreshMode == MetadataRefreshMode.FullRefresh;

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataFields.OfficialRating))
            {
                if (!string.IsNullOrWhiteSpace(data.OfficialRating) || isFullRefresh)
                {
                    video.OfficialRating = data.OfficialRating;
                }
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataFields.Genres))
            {
                if (video.Genres.Length == 0 || isFullRefresh)
                {
                    video.Genres = Array.Empty<string>();

                    foreach (var genre in data.Genres)
                    {
                        video.AddGenre(genre);
                    }
                }
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataFields.Studios))
            {
                if (video.Studios.Length == 0 || isFullRefresh)
                {
                    video.SetStudios(data.Studios);
                }
            }

            if (data.ProductionYear.HasValue)
            {
                if (!video.ProductionYear.HasValue || isFullRefresh)
                {
                    video.ProductionYear = data.ProductionYear;
                }
            }
            if (data.PremiereDate.HasValue)
            {
                if (!video.PremiereDate.HasValue || isFullRefresh)
                {
                    video.PremiereDate = data.PremiereDate;
                }
            }
            if (data.IndexNumber.HasValue)
            {
                if (!video.IndexNumber.HasValue || isFullRefresh)
                {
                    video.IndexNumber = data.IndexNumber;
                }
            }
            if (data.ParentIndexNumber.HasValue)
            {
                if (!video.ParentIndexNumber.HasValue || isFullRefresh)
                {
                    video.ParentIndexNumber = data.ParentIndexNumber;
                }
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataFields.Name))
            {
                if (!string.IsNullOrWhiteSpace(data.Name) && libraryOptions.EnableEmbeddedTitles)
                {
                    // Don't use the embedded name for extras because it will often be the same name as the movie
                    if (!video.ExtraType.HasValue)
                    {
                        video.Name = data.Name;
                    }
                }
            }

            // If we don't have a ProductionYear try and get it from PremiereDate
            if (video.PremiereDate.HasValue && !video.ProductionYear.HasValue)
            {
                video.ProductionYear = video.PremiereDate.Value.ToLocalTime().Year;
            }

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataFields.Overview))
            {
                if (string.IsNullOrWhiteSpace(video.Overview) || isFullRefresh)
                {
                    video.Overview = data.Overview;
                }
            }
        }

        private void FetchPeople(Video video, Model.MediaInfo.MediaInfo data, MetadataRefreshOptions options)
        {
            var isFullRefresh = options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh;

            if (!video.IsLocked && !video.LockedFields.Contains(MetadataFields.Cast))
            {
                if (isFullRefresh || _libraryManager.GetPeople(video).Count == 0)
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
        }

        private SubtitleOptions GetOptions()
        {
            return _config.GetConfiguration<SubtitleOptions>("subtitles");
        }

        /// <summary>
        /// Adds the external subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="currentStreams">The current streams.</param>
        /// <param name="options">The refreshOptions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task AddExternalSubtitles(
            Video video,
            List<MediaStream> currentStreams,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
        {
            var subtitleResolver = new SubtitleResolver(_localization);

            var startIndex = currentStreams.Count == 0 ? 0 : (currentStreams.Select(i => i.Index).Max() + 1);
            var externalSubtitleStreams = subtitleResolver.GetExternalSubtitleStreams(video, startIndex, options.DirectoryService, false);

            var enableSubtitleDownloading = options.MetadataRefreshMode == MetadataRefreshMode.Default ||
                                            options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh;

            var subtitleOptions = GetOptions();

            var libraryOptions = _libraryManager.GetLibraryOptions(video);

            string[] subtitleDownloadLanguages;
            bool skipIfEmbeddedSubtitlesPresent;
            bool skipIfAudioTrackMatches;
            bool requirePerfectMatch;
            bool enabled;

            if (libraryOptions.SubtitleDownloadLanguages == null)
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
                var downloadedLanguages = await new SubtitleDownloader(_logger,
                    _subtitleManager)
                    .DownloadSubtitles(video,
                    currentStreams.Concat(externalSubtitleStreams).ToList(),
                    skipIfEmbeddedSubtitlesPresent,
                    skipIfAudioTrackMatches,
                    requirePerfectMatch,
                    subtitleDownloadLanguages,
                    libraryOptions.DisabledSubtitleFetchers,
                    libraryOptions.SubtitleFetcherOrder,
                    cancellationToken).ConfigureAwait(false);

                // Rescan
                if (downloadedLanguages.Count > 0)
                {
                    externalSubtitleStreams = subtitleResolver.GetExternalSubtitleStreams(video, startIndex, options.DirectoryService, true);
                }
            }

            video.SubtitleFiles = externalSubtitleStreams.Select(i => i.Path).ToArray();

            currentStreams.AddRange(externalSubtitleStreams);
        }

        /// <summary>
        /// Creates dummy chapters.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <return>An array of dummy chapters.</returns>
        private ChapterInfo[] CreateDummyChapters(Video video)
        {
            var runtime = video.RunTimeTicks ?? 0;

            if (runtime < 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} has invalid runtime of {1}",
                        video.Name,
                        runtime));
            }

            if (runtime < _dummyChapterDuration)
            {
                return Array.Empty<ChapterInfo>();
            }

            // Limit to 100 chapters just in case there's some incorrect metadata here
            int chapterCount = (int)Math.Min(runtime / _dummyChapterDuration, 100);
            var chapters = new ChapterInfo[chapterCount];

            long currentChapterTicks = 0;
            for (int i = 0; i < chapterCount; i++)
            {
                chapters[i] = new ChapterInfo
                {
                    StartPositionTicks = currentChapterTicks
                };

                currentChapterTicks += _dummyChapterDuration;
            }

            return chapters;
        }

        private string[] FetchFromDvdLib(Video item)
        {
            var path = item.Path;
            var dvd = new Dvd(path);

            var primaryTitle = dvd.Titles.OrderByDescending(GetRuntime).FirstOrDefault();

            byte? titleNumber = null;

            if (primaryTitle != null)
            {
                titleNumber = primaryTitle.VideoTitleSetNumber;
                item.RunTimeTicks = GetRuntime(primaryTitle);
            }

            return _mediaEncoder.GetPrimaryPlaylistVobFiles(item.Path, null, titleNumber)
                .Select(Path.GetFileName)
                .ToArray();
        }

        private long GetRuntime(Title title)
        {
            return title.ProgramChains
                    .Select(i => (TimeSpan)i.PlaybackTime)
                    .Select(i => i.Ticks)
                    .Sum();
        }
    }
}
