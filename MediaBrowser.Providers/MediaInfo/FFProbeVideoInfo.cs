using DvdLib.Ifo;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    public class FFProbeVideoInfo
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

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public FFProbeVideoInfo(ILogger logger, IIsoManager isoManager, IMediaEncoder mediaEncoder, IItemRepository itemRepo, IBlurayExaminer blurayExaminer, ILocalizationManager localization, IApplicationPaths appPaths, IJsonSerializer json, IEncodingManager encodingManager, IFileSystem fileSystem, IServerConfigurationManager config, ISubtitleManager subtitleManager)
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
        }

        public async Task<ItemUpdateType> ProbeVideo<T>(T item, IDirectoryService directoryService, CancellationToken cancellationToken)
            where T : Video
        {
            var isoMount = await MountIsoIfNeeded(item, cancellationToken).ConfigureAwait(false);

            BlurayDiscInfo blurayDiscInfo = null;

            try
            {
                if (item.VideoType == VideoType.BluRay || (item.IsoType.HasValue && item.IsoType == IsoType.BluRay))
                {
                    var inputPath = isoMount != null ? isoMount.MountedPath : item.Path;

                    blurayDiscInfo = GetBDInfo(inputPath);
                }

                OnPreFetch(item, isoMount, blurayDiscInfo);

                // If we didn't find any satisfying the min length, just take them all
                if (item.VideoType == VideoType.Dvd || (item.IsoType.HasValue && item.IsoType == IsoType.Dvd))
                {
                    if (item.PlayableStreamFileNames.Count == 0)
                    {
                        _logger.Error("No playable vobs found in dvd structure, skipping ffprobe.");
                        return ItemUpdateType.MetadataImport;
                    }
                }

                if (item.VideoType == VideoType.BluRay || (item.IsoType.HasValue && item.IsoType == IsoType.BluRay))
                {
                    if (item.PlayableStreamFileNames.Count == 0)
                    {
                        _logger.Error("No playable vobs found in bluray structure, skipping ffprobe.");
                        return ItemUpdateType.MetadataImport;
                    }
                }

                var result = await GetMediaInfo(item, isoMount, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                FFProbeHelpers.NormalizeFFProbeResult(result);

                cancellationToken.ThrowIfCancellationRequested();

                await Fetch(item, cancellationToken, result, isoMount, blurayDiscInfo, directoryService).ConfigureAwait(false);

            }
            finally
            {
                if (isoMount != null)
                {
                    isoMount.Dispose();
                }
            }

            return ItemUpdateType.MetadataImport;
        }

        private const string SchemaVersion = "1";

        private async Task<InternalMediaInfoResult> GetMediaInfo(BaseItem item, IIsoMount isoMount, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var idString = item.Id.ToString("N");
            var cachePath = Path.Combine(_appPaths.CachePath,
                "ffprobe-video",
                idString.Substring(0, 2), idString, "v" + SchemaVersion + _mediaEncoder.Version + item.DateModified.Ticks.ToString(_usCulture) + ".json");

            try
            {
                return _json.DeserializeFromFile<InternalMediaInfoResult>(cachePath);
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {
            }

            var type = InputType.File;
            var inputPath = isoMount == null ? new[] { item.Path } : new[] { isoMount.MountedPath };

            var video = item as Video;

            if (video != null)
            {
                inputPath = MediaEncoderHelpers.GetInputArgument(video.Path, video.LocationType == LocationType.Remote, video.VideoType, video.IsoType, isoMount, video.PlayableStreamFileNames, out type);
            }

            var result = await _mediaEncoder.GetMediaInfo(inputPath, type, false, cancellationToken).ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
            _json.SerializeToFile(result, cachePath);

            return result;
        }

        protected async Task Fetch(Video video, CancellationToken cancellationToken, InternalMediaInfoResult data, IIsoMount isoMount, BlurayDiscInfo blurayInfo, IDirectoryService directoryService)
        {
            var mediaInfo = MediaEncoderHelpers.GetMediaInfo(data);
            var mediaStreams = mediaInfo.MediaStreams;

            video.TotalBitrate = mediaInfo.TotalBitrate;
            video.FormatName = (mediaInfo.Format ?? string.Empty)
                .Replace("matroska", "mkv", StringComparison.OrdinalIgnoreCase);

            if (data.format != null)
            {
                // For dvd's this may not always be accurate, so don't set the runtime if the item already has one
                var needToSetRuntime = video.VideoType != VideoType.Dvd || video.RunTimeTicks == null || video.RunTimeTicks.Value == 0;

                if (needToSetRuntime && !string.IsNullOrEmpty(data.format.duration))
                {
                    video.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.format.duration, _usCulture)).Ticks;
                }

                if (video.VideoType == VideoType.VideoFile)
                {
                    var extension = (Path.GetExtension(video.Path) ?? string.Empty).TrimStart('.');

                    video.Container = extension;
                }
                else
                {
                    video.Container = null;
                }

                if (!string.IsNullOrEmpty(data.format.size))
                {
                    video.Size = long.Parse(data.format.size, _usCulture);
                }
                else
                {
                    video.Size = null;
                }
            }

            var mediaChapters = (data.Chapters ?? new MediaChapter[] { }).ToList();
            var chapters = mediaChapters.Select(GetChapterInfo).ToList();

            if (video.VideoType == VideoType.BluRay || (video.IsoType.HasValue && video.IsoType.Value == IsoType.BluRay))
            {
                FetchBdInfo(video, chapters, mediaStreams, blurayInfo);
            }

            await AddExternalSubtitles(video, mediaStreams, directoryService, cancellationToken).ConfigureAwait(false);

            FetchWtvInfo(video, data);

            video.IsHD = mediaStreams.Any(i => i.Type == MediaStreamType.Video && i.Width.HasValue && i.Width.Value >= 1270);

            if (chapters.Count == 0 && mediaStreams.Any(i => i.Type == MediaStreamType.Video))
            {
                AddDummyChapters(video, chapters);
            }

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            video.VideoBitRate = videoStream == null ? null : videoStream.BitRate;
            video.DefaultVideoStreamIndex = videoStream == null ? (int?)null : videoStream.Index;

            video.HasSubtitles = mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle);

            ExtractTimestamp(video);

            await _encodingManager.RefreshChapterImages(new ChapterImageRefreshOptions
            {
                Chapters = chapters,
                Video = video,
                ExtractImages = false,
                SaveChapters = false

            }, cancellationToken).ConfigureAwait(false);

            await _itemRepo.SaveMediaStreams(video.Id, mediaStreams, cancellationToken).ConfigureAwait(false);

            await _itemRepo.SaveChapters(video.Id, chapters, cancellationToken).ConfigureAwait(false);
        }

        private ChapterInfo GetChapterInfo(MediaChapter chapter)
        {
            var info = new ChapterInfo();

            if (chapter.tags != null)
            {
                string name;
                if (chapter.tags.TryGetValue("title", out name))
                {
                    info.Name = name;
                }
            }

            info.StartPositionTicks = chapter.start / 100;

            return info;
        }

        private void FetchBdInfo(BaseItem item, List<ChapterInfo> chapters, List<MediaStream> mediaStreams, BlurayDiscInfo blurayInfo)
        {
            var video = (Video)item;

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

            video.MainFeaturePlaylistName = blurayInfo.PlaylistName;

            if (blurayInfo.RunTimeTicks.HasValue && blurayInfo.RunTimeTicks.Value > 0)
            {
                video.RunTimeTicks = blurayInfo.RunTimeTicks;
            }

            video.PlayableStreamFileNames = blurayInfo.Files.ToList();

            if (blurayInfo.Chapters != null)
            {
                chapters.Clear();

                chapters.AddRange(blurayInfo.Chapters.Select(c => new ChapterInfo
                {
                    StartPositionTicks = TimeSpan.FromSeconds(c).Ticks

                }));
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
            return _blurayExaminer.GetDiscInfo(path);
        }

        private void FetchWtvInfo(Video video, InternalMediaInfoResult data)
        {
            if (data.format == null || data.format.tags == null)
            {
                return;
            }

            if (video.Genres.Count == 0)
            {
                if (!video.LockedFields.Contains(MetadataFields.Genres))
                {
                    var genres = FFProbeHelpers.GetDictionaryValue(data.format.tags, "genre");

                    if (!string.IsNullOrEmpty(genres))
                    {
                        video.Genres = genres.Split(new[] { ';', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(i => !string.IsNullOrWhiteSpace(i))
                            .Select(i => i.Trim())
                            .ToList();
                    }
                }
            }

            if (string.IsNullOrEmpty(video.Overview))
            {
                if (!video.LockedFields.Contains(MetadataFields.Overview))
                {
                    var overview = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/SubTitleDescription");

                    if (!string.IsNullOrWhiteSpace(overview))
                    {
                        video.Overview = overview;
                    }
                }
            }

            if (string.IsNullOrEmpty(video.OfficialRating))
            {
                var officialRating = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/ParentalRating");

                if (!string.IsNullOrWhiteSpace(officialRating))
                {
                    if (!video.LockedFields.Contains(MetadataFields.OfficialRating))
                    {
                        video.OfficialRating = officialRating;
                    }
                }
            }

            if (video.People.Count == 0)
            {
                if (!video.LockedFields.Contains(MetadataFields.Cast))
                {
                    var people = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/MediaCredits");

                    if (!string.IsNullOrEmpty(people))
                    {
                        video.People = people.Split(new[] { ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(i => !string.IsNullOrWhiteSpace(i))
                            .Select(i => new PersonInfo { Name = i.Trim(), Type = PersonType.Actor })
                            .ToList();
                    }
                }
            }

            if (!video.ProductionYear.HasValue)
            {
                var year = FFProbeHelpers.GetDictionaryValue(data.format.tags, "WM/OriginalReleaseTime");

                if (!string.IsNullOrWhiteSpace(year))
                {
                    int val;

                    if (int.TryParse(year, NumberStyles.Integer, _usCulture, out val))
                    {
                        video.ProductionYear = val;
                    }
                }
            }
        }

        private IEnumerable<string> SubtitleExtensions
        {
            get
            {
                return new[] { ".srt", ".ssa", ".ass", ".sub" };
            }
        }

        public IEnumerable<FileSystemInfo> GetSubtitleFiles(Video video, IDirectoryService directoryService, bool clearCache)
        {
            var containingPath = video.ContainingFolderPath;

            if (string.IsNullOrEmpty(containingPath))
            {
                throw new ArgumentException(string.Format("Cannot search for items that don't have a path: {0} {1}", video.Name, video.Id));
            }

            var files = directoryService.GetFiles(containingPath, clearCache);

            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            return files.Where(i =>
            {
                if (!i.Attributes.HasFlag(FileAttributes.Directory) &&
                    SubtitleExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    var fullName = i.FullName;

                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);

                    if (string.Equals(videoFileNameWithoutExtension, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension + ".", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// Adds the external subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="currentStreams">The current streams.</param>
        private async Task AddExternalSubtitles(Video video, List<MediaStream> currentStreams, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var externalSubtitleStreams = GetExternalSubtitleStreams(video, currentStreams.Count, directoryService, false).ToList();

            if ((_config.Configuration.SubtitleOptions.DownloadEpisodeSubtitles &&
                video is Episode) ||
                (_config.Configuration.SubtitleOptions.DownloadMovieSubtitles &&
                video is Movie))
            {
                var downloadedLanguages = await new SubtitleDownloader(_logger,
                    _subtitleManager)
                    .DownloadSubtitles(video,
                    currentStreams,
                    externalSubtitleStreams,
                    _config.Configuration.SubtitleOptions.SkipIfGraphicalSubtitlesPresent,
                    _config.Configuration.SubtitleOptions.SkipIfAudioTrackMatches,
                    _config.Configuration.SubtitleOptions.DownloadLanguages,
                    cancellationToken).ConfigureAwait(false);

                // Rescan
                if (downloadedLanguages.Count > 0)
                {
                    externalSubtitleStreams = GetExternalSubtitleStreams(video, currentStreams.Count, directoryService, true).ToList();
                }
            }

            video.SubtitleFiles = externalSubtitleStreams.Select(i => i.Path).OrderBy(i => i).ToList();

            currentStreams.AddRange(externalSubtitleStreams);
        }

        private IEnumerable<MediaStream> GetExternalSubtitleStreams(Video video, 
            int startIndex, 
            IDirectoryService directoryService,
            bool clearCache)
        {
            var files = GetSubtitleFiles(video, directoryService, clearCache);

            var streams = new List<MediaStream>();

            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            foreach (var file in files)
            {
                var fullName = file.FullName;

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);

                // If the subtitle file matches the video file name
                if (string.Equals(videoFileNameWithoutExtension, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {
                    streams.Add(new MediaStream
                    {
                        Index = startIndex++,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = fullName,
                        Codec = Path.GetExtension(fullName).ToLower().TrimStart('.')
                    });
                }
                else if (fileNameWithoutExtension.StartsWith(videoFileNameWithoutExtension + ".", StringComparison.OrdinalIgnoreCase))
                {
                    // Support xbmc naming conventions - 300.spanish.srt
                    var language = fileNameWithoutExtension.Split('.').LastOrDefault();

                    // Try to translate to three character code
                    // Be flexible and check against both the full and three character versions
                    var culture = _localization.GetCultures()
                        .FirstOrDefault(i => string.Equals(i.DisplayName, language, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Name, language, StringComparison.OrdinalIgnoreCase) || string.Equals(i.ThreeLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase) || string.Equals(i.TwoLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase));

                    if (culture != null)
                    {
                        language = culture.ThreeLetterISOLanguageName;
                    }

                    streams.Add(new MediaStream
                    {
                        Index = startIndex++,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = fullName,
                        Codec = Path.GetExtension(fullName).ToLower().TrimStart('.'),
                        Language = language
                    });
                }
            }

            return streams;
        }

        /// <summary>
        /// The dummy chapter duration
        /// </summary>
        private readonly long _dummyChapterDuration = TimeSpan.FromMinutes(5).Ticks;

        /// <summary>
        /// Adds the dummy chapters.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="chapters">The chapters.</param>
        private void AddDummyChapters(Video video, List<ChapterInfo> chapters)
        {
            var runtime = video.RunTimeTicks ?? 0;

            if (runtime < 0)
            {
                throw new ArgumentException(string.Format("{0} has invalid runtime of {1}", video.Name, runtime));
            }

            if (runtime < _dummyChapterDuration)
            {
                return;
            }

            long currentChapterTicks = 0;
            var index = 1;

            // Limit to 100 chapters just in case there's some incorrect metadata here
            while (currentChapterTicks < runtime && index < 100)
            {
                chapters.Add(new ChapterInfo
                {
                    Name = "Chapter " + index,
                    StartPositionTicks = currentChapterTicks
                });

                index++;
                currentChapterTicks += _dummyChapterDuration;
            }
        }

        /// <summary>
        /// Called when [pre fetch].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="mount">The mount.</param>
        private void OnPreFetch(Video item, IIsoMount mount, BlurayDiscInfo blurayDiscInfo)
        {
            if (item.VideoType == VideoType.Iso)
            {
                item.IsoType = DetermineIsoType(mount);
            }

            if (item.VideoType == VideoType.Dvd || (item.IsoType.HasValue && item.IsoType == IsoType.Dvd))
            {
                FetchFromDvdLib(item, mount);
            }

            if (item.VideoType == VideoType.BluRay || (item.IsoType.HasValue && item.IsoType.Value == IsoType.BluRay))
            {
                item.PlayableStreamFileNames = blurayDiscInfo.Files.ToList();
            }
        }

        private void ExtractTimestamp(Video video)
        {
            if (video.VideoType == VideoType.VideoFile)
            {
                if (string.Equals(video.Container, "mpeg2ts", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(video.Container, "m2ts", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(video.Container, "ts", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        video.Timestamp = GetMpegTimestamp(video.Path);

                        _logger.Debug("Video has {0} timestamp", video.Timestamp);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error extracting timestamp info from {0}", ex, video.Path);
                        video.Timestamp = null;
                    }
                }
            }
        }

        private TransportStreamTimestamp GetMpegTimestamp(string path)
        {
            var packetBuffer = new byte['Å'];

            using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Read(packetBuffer, 0, packetBuffer.Length);
            }

            if (packetBuffer[0] == 71)
            {
                return TransportStreamTimestamp.None;
            }

            if ((packetBuffer[4] == 71) && (packetBuffer['Ä'] == 71))
            {
                if ((packetBuffer[0] == 0) && (packetBuffer[1] == 0) && (packetBuffer[2] == 0) && (packetBuffer[3] == 0))
                {
                    return TransportStreamTimestamp.Zero;
                }

                return TransportStreamTimestamp.Valid;
            }

            return TransportStreamTimestamp.None;
        }

        private void FetchFromDvdLib(Video item, IIsoMount mount)
        {
            var path = mount == null ? item.Path : mount.MountedPath;
            var dvd = new Dvd(path);

            var primaryTitle = dvd.Titles.OrderByDescending(GetRuntime).FirstOrDefault();

            byte? titleNumber = null;

            if (primaryTitle != null)
            {
                titleNumber = primaryTitle.VideoTitleSetNumber;
                item.RunTimeTicks = GetRuntime(primaryTitle);
            }

            item.PlayableStreamFileNames = GetPrimaryPlaylistVobFiles(item, mount, titleNumber)
                .Select(Path.GetFileName)
                .ToList();
        }

        private long GetRuntime(Title title)
        {
            return title.ProgramChains
                    .Select(i => (TimeSpan)i.PlaybackTime)
                    .Select(i => i.Ticks)
                    .Sum();
        }

        /// <summary>
        /// Mounts the iso if needed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IsoMount.</returns>
        protected Task<IIsoMount> MountIsoIfNeeded(Video item, CancellationToken cancellationToken)
        {
            if (item.VideoType == VideoType.Iso)
            {
                return _isoManager.Mount(item.Path, cancellationToken);
            }

            return Task.FromResult<IIsoMount>(null);
        }

        /// <summary>
        /// Determines the type of the iso.
        /// </summary>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>System.Nullable{IsoType}.</returns>
        private IsoType? DetermineIsoType(IIsoMount isoMount)
        {
            var folders = Directory.EnumerateDirectories(isoMount.MountedPath).Select(Path.GetFileName).ToList();

            if (folders.Contains("video_ts", StringComparer.OrdinalIgnoreCase))
            {
                return IsoType.Dvd;
            }
            if (folders.Contains("bdmv", StringComparer.OrdinalIgnoreCase))
            {
                return IsoType.BluRay;
            }

            return null;
        }

        private IEnumerable<string> GetPrimaryPlaylistVobFiles(Video video, IIsoMount isoMount, uint? titleNumber)
        {
            // min size 300 mb
            const long minPlayableSize = 314572800;

            var root = isoMount != null ? isoMount.MountedPath : video.Path;

            // Try to eliminate menus and intros by skipping all files at the front of the list that are less than the minimum size
            // Once we reach a file that is at least the minimum, return all subsequent ones
            var allVobs = new DirectoryInfo(root).EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(file => string.Equals(file.Extension, ".vob", StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.FullName)
                .ToList();

            // If we didn't find any satisfying the min length, just take them all
            if (allVobs.Count == 0)
            {
                _logger.Error("No vobs found in dvd structure.");
                return new List<string>();
            }

            if (titleNumber.HasValue)
            {
                var prefix = string.Format("VTS_0{0}_", titleNumber.Value.ToString(_usCulture));
                var vobs = allVobs.Where(i => i.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

                if (vobs.Count > 0)
                {
                    var minSizeVobs = vobs
                        .SkipWhile(f => f.Length < minPlayableSize)
                        .ToList();

                    return minSizeVobs.Count == 0 ? vobs.Select(i => i.FullName) : minSizeVobs.Select(i => i.FullName);
                }

                _logger.Debug("Could not determine vob file list for {0} using DvdLib. Will scan using file sizes.", video.Path);
            }

            var files = allVobs
                .SkipWhile(f => f.Length < minPlayableSize)
                .ToList();

            // If we didn't find any satisfying the min length, just take them all
            if (files.Count == 0)
            {
                _logger.Warn("Vob size filter resulted in zero matches. Taking all vobs.");
                files = allVobs;
            }

            // Assuming they're named "vts_05_01", take all files whose second part matches that of the first file
            if (files.Count > 0)
            {
                var parts = Path.GetFileNameWithoutExtension(files[0].FullName).Split('_');

                if (parts.Length == 3)
                {
                    var title = parts[1];

                    files = files.TakeWhile(f =>
                    {
                        var fileParts = Path.GetFileNameWithoutExtension(f.FullName).Split('_');

                        return fileParts.Length == 3 && string.Equals(title, fileParts[1], StringComparison.OrdinalIgnoreCase);

                    }).ToList();

                    // If this resulted in not getting any vobs, just take them all
                    if (files.Count == 0)
                    {
                        _logger.Warn("Vob filename filter resulted in zero matches. Taking all vobs.");
                        files = allVobs;
                    }
                }
            }

            return files.Select(i => i.FullName);
        }
    }
}
