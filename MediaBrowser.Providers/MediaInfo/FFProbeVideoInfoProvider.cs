using DvdLib.Ifo;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
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
    /// <summary>
    /// Extracts video information using ffprobe
    /// </summary>
    public class FFProbeVideoInfoProvider : BaseFFProbeProvider<Video>
    {
        private readonly IItemRepository _itemRepo;

        public FFProbeVideoInfoProvider(IIsoManager isoManager, IBlurayExaminer blurayExaminer, IJsonSerializer jsonSerializer, ILogManager logManager, IServerConfigurationManager configurationManager, IMediaEncoder mediaEncoder, ILocalizationManager localization, IItemRepository itemRepo)
            : base(logManager, configurationManager, mediaEncoder, jsonSerializer)
        {
            if (isoManager == null)
            {
                throw new ArgumentNullException("isoManager");
            }
            if (blurayExaminer == null)
            {
                throw new ArgumentNullException("blurayExaminer");
            }

            _blurayExaminer = blurayExaminer;
            _localization = localization;
            _itemRepo = itemRepo;
            _isoManager = isoManager;
        }

        /// <summary>
        /// Gets or sets the bluray examiner.
        /// </summary>
        /// <value>The bluray examiner.</value>
        private readonly IBlurayExaminer _blurayExaminer;

        /// <summary>
        /// The _iso manager
        /// </summary>
        private readonly IIsoManager _isoManager;

        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Returns true or false indicating if the provider should refresh when the contents of it's directory changes
        /// </summary>
        /// <value><c>true</c> if [refresh on file system stamp change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                // Need this in case external subtitle files change
                return true;
            }
        }

        /// <summary>
        /// Gets the filestamp extensions.
        /// </summary>
        /// <value>The filestamp extensions.</value>
        protected override string[] FilestampExtensions
        {
            get
            {
                return new[] { ".srt", ".ssa", ".ass" };
            }
        }

        public override MetadataProviderPriority Priority
        {
            get
            {
                return MetadataProviderPriority.Second;
            }
        }

        /// <summary>
        /// Supports video files and dvd structures
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return false;
            }

            var video = item as Video;

            if (video != null)
            {
                if (video.VideoType == VideoType.Iso)
                {
                    return _isoManager.CanMount(item.Path);
                }

                return video.VideoType == VideoType.VideoFile || video.VideoType == VideoType.Dvd || video.VideoType == VideoType.BluRay;
            }

            return false;
        }

        /// <summary>
        /// Called when [pre fetch].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="mount">The mount.</param>
        protected override void OnPreFetch(Video item, IIsoMount mount)
        {
            if (item.VideoType == VideoType.Iso)
            {
                item.IsoType = DetermineIsoType(mount);
            }

            if (item.VideoType == VideoType.Dvd || (item.IsoType.HasValue && item.IsoType == IsoType.Dvd))
            {
                FetchFromDvdLib(item, mount);
            }

            base.OnPreFetch(item, mount);
        }

        private void FetchFromDvdLib(Video item, IIsoMount mount)
        {
            var path = mount == null ? item.Path : mount.MountedPath;
            var dvd = new Dvd(path);

            item.RunTimeTicks = dvd.Titles.Select(GetRuntime).Max();

            PopulateDvdStreamFiles(item, mount);
        }

        private long GetRuntime(Title title)
        {
            return title.ProgramChains
                    .Select(i => (TimeSpan)i.PlaybackTime)
                    .Select(i => i.Ticks)
                    .Sum();
        }

        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            var isoMount = await MountIsoIfNeeded(video, cancellationToken).ConfigureAwait(false);

            try
            {
                OnPreFetch(video, isoMount);

                // If we didn't find any satisfying the min length, just take them all
                if (video.VideoType == VideoType.Dvd || (video.IsoType.HasValue && video.IsoType == IsoType.Dvd))
                {
                    if (video.PlayableStreamFileNames.Count == 0)
                    {
                        Logger.Error("No playable vobs found in dvd structure, skipping ffprobe.");
                        SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
                        return true;
                    }
                }

                var result = await GetMediaInfo(item, isoMount, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                NormalizeFFProbeResult(result);

                cancellationToken.ThrowIfCancellationRequested();

                await Fetch(video, force, providerInfo, cancellationToken, result, isoMount).ConfigureAwait(false);

            }
            finally
            {
                if (isoMount != null)
                {
                    isoMount.Dispose();
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        /// <summary>
        /// Mounts the iso if needed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IsoMount.</returns>
        protected override Task<IIsoMount> MountIsoIfNeeded(Video item, CancellationToken cancellationToken)
        {
            if (item.VideoType == VideoType.Iso)
            {
                return _isoManager.Mount(item.Path, cancellationToken);
            }

            return base.MountIsoIfNeeded(item, cancellationToken);
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

        /// <summary>
        /// Finds vob files and populates the dvd stream file properties
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="isoMount">The iso mount.</param>
        private void PopulateDvdStreamFiles(Video video, IIsoMount isoMount)
        {
            // min size 300 mb
            const long minPlayableSize = 314572800;

            var root = isoMount != null ? isoMount.MountedPath : video.Path;

            // Try to eliminate menus and intros by skipping all files at the front of the list that are less than the minimum size
            // Once we reach a file that is at least the minimum, return all subsequent ones
            var allVobs = Directory.EnumerateFiles(root, "*.vob", SearchOption.AllDirectories).ToList();

            // If we didn't find any satisfying the min length, just take them all
            if (allVobs.Count == 0)
            {
                Logger.Error("No vobs found in dvd structure.");
                return;
            }

            var files = allVobs
                .SkipWhile(f => new FileInfo(f).Length < minPlayableSize)
                .ToList();

            // If we didn't find any satisfying the min length, just take them all
            if (files.Count == 0)
            {
                Logger.Warn("Vob size filter resulted in zero matches. Taking all vobs.");
                files = allVobs;
            }

            // Assuming they're named "vts_05_01", take all files whose second part matches that of the first file
            if (files.Count > 0)
            {
                var parts = Path.GetFileNameWithoutExtension(files[0]).Split('_');

                if (parts.Length == 3)
                {
                    var title = parts[1];

                    files = files.TakeWhile(f =>
                    {
                        var fileParts = Path.GetFileNameWithoutExtension(f).Split('_');

                        return fileParts.Length == 3 && string.Equals(title, fileParts[1], StringComparison.OrdinalIgnoreCase);

                    }).ToList();

                    // If this resulted in not getting any vobs, just take them all
                    if (files.Count == 0)
                    {
                        Logger.Warn("Vob filename filter resulted in zero matches. Taking all vobs.");
                        files = allVobs;
                    }
                }
            }

            video.PlayableStreamFileNames = files.Select(Path.GetFileName).ToList();
        }

        /// <summary>
        /// Fetches the specified video.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="data">The data.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>Task.</returns>
        protected async Task Fetch(Video video, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken, MediaInfoResult data, IIsoMount isoMount)
        {
            if (data.format != null)
            {
                // For dvd's this may not always be accurate, so don't set the runtime if the item already has one
                var needToSetRuntime = video.VideoType != VideoType.Dvd || video.RunTimeTicks == null || video.RunTimeTicks.Value == 0;

                if (needToSetRuntime && !string.IsNullOrEmpty(data.format.duration))
                {
                    video.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.format.duration, UsCulture)).Ticks;
                }
            }

            var mediaStreams = MediaEncoderHelpers.GetMediaStreams(data).ToList();

            var chapters = data.Chapters ?? new List<ChapterInfo>();

            if (video.VideoType == VideoType.BluRay || (video.IsoType.HasValue && video.IsoType.Value == IsoType.BluRay))
            {
                var inputPath = isoMount != null ? isoMount.MountedPath : video.Path;
                FetchBdInfo(video, chapters, mediaStreams, inputPath, cancellationToken);
            }

            AddExternalSubtitles(video, mediaStreams);

            FetchWtvInfo(video, force, data);

            video.IsHD = mediaStreams.Any(i => i.Type == MediaStreamType.Video && i.Width.HasValue && i.Width.Value >= 1270);

            if (chapters.Count == 0 && mediaStreams.Any(i => i.Type == MediaStreamType.Video))
            {
                AddDummyChapters(video, chapters);
            }

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

            video.VideoBitRate = videoStream == null ? null : videoStream.BitRate;
            video.DefaultVideoStreamIndex = videoStream == null ? (int?)null : videoStream.Index;

            video.HasSubtitles = mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle);

            await FFMpegManager.Instance.PopulateChapterImages(video, chapters, false, false, cancellationToken).ConfigureAwait(false);

            var videoFileChanged = CompareDate(video) > providerInfo.LastRefreshed;

            await _itemRepo.SaveMediaStreams(video.Id, mediaStreams, cancellationToken).ConfigureAwait(false);

            // Only save chapters if forcing, if the video changed, or if there are not already any saved ones
            if (force || videoFileChanged || _itemRepo.GetChapter(video.Id, 0) == null)
            {
                await _itemRepo.SaveChapters(video.Id, chapters, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Fetches the WTV info.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="data">The data.</param>
        private void FetchWtvInfo(Video video, bool force, MediaInfoResult data)
        {
            if (data.format == null || data.format.tags == null)
            {
                return;
            }

            if (force || video.Genres.Count == 0)
            {
                if (!video.LockedFields.Contains(MetadataFields.Genres))
                {
                    var genres = GetDictionaryValue(data.format.tags, "genre");

                    if (!string.IsNullOrEmpty(genres))
                    {
                        video.Genres = genres.Split(new[] { ';', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(i => !string.IsNullOrWhiteSpace(i))
                            .Select(i => i.Trim())
                            .ToList();
                    }
                }
            }

            if (force || string.IsNullOrEmpty(video.Overview))
            {
                if (!video.LockedFields.Contains(MetadataFields.Overview))
                {
                    var overview = GetDictionaryValue(data.format.tags, "WM/SubTitleDescription");

                    if (!string.IsNullOrWhiteSpace(overview))
                    {
                        video.Overview = overview;
                    }
                }
            }

            if (force || string.IsNullOrEmpty(video.OfficialRating))
            {
                var officialRating = GetDictionaryValue(data.format.tags, "WM/ParentalRating");

                if (!string.IsNullOrWhiteSpace(officialRating))
                {
                    if (!video.LockedFields.Contains(MetadataFields.OfficialRating))
                    {
                        video.OfficialRating = officialRating;
                    }
                }
            }

            if (force || video.People.Count == 0)
            {
                if (!video.LockedFields.Contains(MetadataFields.Cast))
                {
                    var people = GetDictionaryValue(data.format.tags, "WM/MediaCredits");

                    if (!string.IsNullOrEmpty(people))
                    {
                        video.People = people.Split(new[] { ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(i => !string.IsNullOrWhiteSpace(i))
                            .Select(i => new PersonInfo { Name = i.Trim(), Type = PersonType.Actor })
                            .ToList();
                    }
                }
            }

            if (force || !video.ProductionYear.HasValue)
            {
                var year = GetDictionaryValue(data.format.tags, "WM/OriginalReleaseTime");

                if (!string.IsNullOrWhiteSpace(year))
                {
                    int val;

                    if (int.TryParse(year, NumberStyles.Integer, UsCulture, out val))
                    {
                        video.ProductionYear = val;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the external subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="currentStreams">The current streams.</param>
        private void AddExternalSubtitles(Video video, List<MediaStream> currentStreams)
        {
            var useParent = !video.ResolveArgs.IsDirectory;

            if (useParent && video.Parent == null)
            {
                return;
            }

            var fileSystemChildren = useParent
                                         ? video.Parent.ResolveArgs.FileSystemChildren
                                         : video.ResolveArgs.FileSystemChildren;

            var startIndex = currentStreams.Count;
            var streams = new List<MediaStream>();

            var videoFileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            foreach (var file in fileSystemChildren
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Directory) && FilestampExtensions.Contains(Path.GetExtension(f.FullName), StringComparer.OrdinalIgnoreCase)))
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

            currentStreams.AddRange(streams);
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
        /// Fetches the bd info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="chapters">The chapters.</param>
        /// <param name="mediaStreams">The media streams.</param>
        /// <param name="inputPath">The input path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void FetchBdInfo(BaseItem item, List<ChapterInfo> chapters, List<MediaStream> mediaStreams, string inputPath, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            var result = GetBDInfo(inputPath);

            cancellationToken.ThrowIfCancellationRequested();

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
            Fetch(video, mediaStreams, result, chapters);

            videoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

            // Use the ffprobe values if these are empty
            if (videoStream != null)
            {
                videoStream.BitRate = IsEmpty(videoStream.BitRate) ? currentBitRate : videoStream.BitRate;
                videoStream.Width = IsEmpty(videoStream.Width) ? currentWidth : videoStream.Width;
                videoStream.Height = IsEmpty(videoStream.Height) ? currentHeight : videoStream.Height;
            }
        }

        /// <summary>
        /// Determines whether the specified num is empty.
        /// </summary>
        /// <param name="num">The num.</param>
        /// <returns><c>true</c> if the specified num is empty; otherwise, <c>false</c>.</returns>
        private bool IsEmpty(int? num)
        {
            return !num.HasValue || num.Value == 0;
        }

        /// <summary>
        /// Fills video properties from the VideoStream of the largest playlist
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="mediaStreams">The media streams.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="chapters">The chapters.</param>
        private void Fetch(Video video, List<MediaStream> mediaStreams, BlurayDiscInfo stream, List<ChapterInfo> chapters)
        {
            // Check all input for null/empty/zero

            mediaStreams.Clear();
            mediaStreams.AddRange(stream.MediaStreams);

            video.MainFeaturePlaylistName = stream.PlaylistName;

            if (stream.RunTimeTicks.HasValue && stream.RunTimeTicks.Value > 0)
            {
                video.RunTimeTicks = stream.RunTimeTicks;
            }

            video.PlayableStreamFileNames = stream.Files.ToList();

            if (stream.Chapters != null)
            {
                chapters.Clear();

                chapters.AddRange(stream.Chapters.Select(c => new ChapterInfo
                {
                    StartPositionTicks = TimeSpan.FromSeconds(c).Ticks

                }));
            }
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
    }
}
