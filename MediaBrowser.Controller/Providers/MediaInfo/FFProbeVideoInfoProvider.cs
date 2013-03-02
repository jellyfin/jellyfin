using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Extracts video information using ffprobe
    /// </summary>
    public class FFProbeVideoInfoProvider : BaseFFProbeProvider<Video>
    {
        /// <summary>
        /// Gets or sets the bd info cache.
        /// </summary>
        /// <value>The bd info cache.</value>
        private FileSystemRepository BdInfoCache { get; set; }

        /// <summary>
        /// Gets or sets the bluray examiner.
        /// </summary>
        /// <value>The bluray examiner.</value>
        private readonly IBlurayExaminer _blurayExaminer;

        /// <summary>
        /// The _iso manager
        /// </summary>
        private readonly IIsoManager _isoManager;

        /// <summary>
        /// The _protobuf serializer
        /// </summary>
        private readonly IProtobufSerializer _protobufSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFProbeVideoInfoProvider" /> class.
        /// </summary>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="blurayExaminer">The bluray examiner.</param>
        /// <param name="protobufSerializer">The protobuf serializer.</param>
        /// <exception cref="System.ArgumentNullException">blurayExaminer</exception>
        public FFProbeVideoInfoProvider(IIsoManager isoManager, IBlurayExaminer blurayExaminer, IProtobufSerializer protobufSerializer, ILogManager logManager)
            : base(logManager)
        {
            if (isoManager == null)
            {
                throw new ArgumentNullException("isoManager");
            }
            if (blurayExaminer == null)
            {
                throw new ArgumentNullException("blurayExaminer");
            }
            if (protobufSerializer == null)
            {
                throw new ArgumentNullException("protobufSerializer");
            }

            _blurayExaminer = blurayExaminer;
            _isoManager = isoManager;
            _protobufSerializer = protobufSerializer;

            BdInfoCache = new FileSystemRepository(Path.Combine(Kernel.Instance.ApplicationPaths.CachePath, "bdinfo"));
        }

        /// <summary>
        /// Returns true or false indicating if the provider should refresh when the contents of it's directory changes
        /// </summary>
        /// <value><c>true</c> if [refresh on file system stamp change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Supports video files and dvd structures
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
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
                PopulateDvdStreamFiles(item, mount);
            }

            base.OnPreFetch(item, mount);
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
            video.PlayableStreamFileNames = Directory.EnumerateFiles(root, "*.vob", SearchOption.AllDirectories).SkipWhile(f => new FileInfo(f).Length < minPlayableSize).Select(Path.GetFileName).ToList();
        }

        /// <summary>
        /// Fetches the specified video.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="data">The data.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>Task.</returns>
        protected override Task Fetch(Video video, CancellationToken cancellationToken, FFProbeResult data, IIsoMount isoMount)
        {
            return Task.Run(() =>
            {
                if (data.format != null)
                {
                    // For dvd's this may not always be accurate, so don't set the runtime if the item already has one
                    var needToSetRuntime = video.VideoType != VideoType.Dvd || video.RunTimeTicks == null || video.RunTimeTicks.Value == 0;

                    if (needToSetRuntime && !string.IsNullOrEmpty(data.format.duration))
                    {
                        video.RunTimeTicks = TimeSpan.FromSeconds(double.Parse(data.format.duration)).Ticks;
                    }
                }

                if (data.streams != null)
                {
                    video.MediaStreams = data.streams.Select(s => GetMediaStream(s, data.format)).ToList();
                }

                if (data.Chapters != null)
                {
                    video.Chapters = data.Chapters;
                }

                if (video.Chapters == null || video.Chapters.Count == 0)
                {
                    AddDummyChapters(video);
                }

                if (video.VideoType == VideoType.BluRay || (video.IsoType.HasValue && video.IsoType.Value == IsoType.BluRay))
                {
                    var inputPath = isoMount != null ? isoMount.MountedPath : video.Path;
                    FetchBdInfo(video, inputPath, BdInfoCache, cancellationToken);
                }

                AddExternalSubtitles(video);
            });
        }

        /// <summary>
        /// Adds the external subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        private void AddExternalSubtitles(Video video)
        {
            var useParent = (video.VideoType == VideoType.VideoFile || video.VideoType == VideoType.Iso) && !(video is Movie);

            if (useParent && video.Parent == null)
            {
                return;
            }

            var fileSystemChildren = useParent
                                         ? video.Parent.ResolveArgs.FileSystemChildren
                                         : video.ResolveArgs.FileSystemChildren;

            var startIndex = video.MediaStreams == null ? 0 : video.MediaStreams.Count;
            var streams = new List<MediaStream>();

            foreach (var file in fileSystemChildren.Where(f => !f.IsDirectory))
            {
                var extension = Path.GetExtension(file.Path);

                if (string.Equals(extension, ".srt", StringComparison.OrdinalIgnoreCase))
                {
                    streams.Add(new MediaStream
                    {
                        Index = startIndex,
                        Type = MediaStreamType.Subtitle,
                        IsExternal = true,
                        Path = file.Path,
                        Codec = "srt"
                    });

                    startIndex++;
                }
            }

            if (video.MediaStreams == null)
            {
                video.MediaStreams = new List<MediaStream>();
            }
            video.MediaStreams.AddRange(streams);
        }

        /// <summary>
        /// The dummy chapter duration
        /// </summary>
        private readonly long DummyChapterDuration = TimeSpan.FromMinutes(10).Ticks;

        /// <summary>
        /// Adds the dummy chapters.
        /// </summary>
        /// <param name="video">The video.</param>
        private void AddDummyChapters(Video video)
        {
            var runtime = video.RunTimeTicks ?? 0;

            if (runtime < DummyChapterDuration)
            {
                return;
            }

            long currentChapterTicks = 0;
            var index = 1;

            var chapters = new List<ChapterInfo> { };

            while (currentChapterTicks < runtime)
            {
                chapters.Add(new ChapterInfo
                {
                    Name = "Chapter " + index,
                    StartPositionTicks = currentChapterTicks
                });

                index++;
                currentChapterTicks += DummyChapterDuration;
            }

            video.Chapters = chapters;
        }

        /// <summary>
        /// Fetches the bd info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="inputPath">The input path.</param>
        /// <param name="bdInfoCache">The bd info cache.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void FetchBdInfo(BaseItem item, string inputPath, FileSystemRepository bdInfoCache, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            // Get the path to the cache file
            var cacheName = item.Id + "_" + item.DateModified.Ticks;

            var cacheFile = bdInfoCache.GetResourcePath(cacheName, ".pb");

            BlurayDiscInfo result;

            try
            {
                result = _protobufSerializer.DeserializeFromFile<BlurayDiscInfo>(cacheFile);
            }
            catch (FileNotFoundException)
            {
                result = GetBDInfo(inputPath);

                _protobufSerializer.SerializeToFile(result, cacheFile);
            }

            cancellationToken.ThrowIfCancellationRequested();

            int? currentHeight = null;
            int? currentWidth = null;
            int? currentBitRate = null;

            var videoStream = video.MediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

            // Grab the values that ffprobe recorded
            if (videoStream != null)
            {
                currentBitRate = videoStream.BitRate;
                currentWidth = videoStream.Width;
                currentHeight = videoStream.Height;
            }

            // Fill video properties from the BDInfo result
            Fetch(video, inputPath, result);

            videoStream = video.MediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

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
        /// <param name="inputPath">The input path.</param>
        /// <param name="stream">The stream.</param>
        private void Fetch(Video video, string inputPath, BlurayDiscInfo stream)
        {
            // Check all input for null/empty/zero

            video.MediaStreams = stream.MediaStreams;

            if (stream.RunTimeTicks.HasValue && stream.RunTimeTicks.Value > 0)
            {
                video.RunTimeTicks = stream.RunTimeTicks;
            }

            video.PlayableStreamFileNames = stream.Files.ToList();

            if (stream.Chapters != null)
            {
                video.Chapters = stream.Chapters.Select(c => new ChapterInfo
                {
                    StartPositionTicks = TimeSpan.FromSeconds(c).Ticks

                }).ToList();
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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                BdInfoCache.Dispose();
            }

            base.Dispose(dispose);
        }
    }
}
