using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaInfo
{
    /// <summary>
    /// Class FFMpegManager
    /// </summary>
    public class FFMpegManager
    {
        /// <summary>
        /// Gets or sets the video image cache.
        /// </summary>
        /// <value>The video image cache.</value>
        internal FileSystemRepository VideoImageCache { get; set; }

        /// <summary>
        /// Gets or sets the subtitle cache.
        /// </summary>
        /// <value>The subtitle cache.</value>
        internal FileSystemRepository SubtitleCache { get; set; }

        private readonly IServerApplicationPaths _appPaths;
        private readonly IMediaEncoder _encoder;
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFMpegManager" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="encoder">The encoder.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="itemRepo">The item repo.</param>
        /// <exception cref="System.ArgumentNullException">zipClient</exception>
        public FFMpegManager(IServerApplicationPaths appPaths, IMediaEncoder encoder, ILogger logger, IItemRepository itemRepo, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _encoder = encoder;
            _logger = logger;
            _itemRepo = itemRepo;
            _fileSystem = fileSystem;

            VideoImageCache = new FileSystemRepository(VideoImagesDataPath);
            SubtitleCache = new FileSystemRepository(SubtitleCachePath);
        }

        /// <summary>
        /// Gets the video images data path.
        /// </summary>
        /// <value>The video images data path.</value>
        public string VideoImagesDataPath
        {
            get
            {
                return Path.Combine(_appPaths.DataPath, "extracted-video-images");
            }
        }

        /// <summary>
        /// Gets the audio images data path.
        /// </summary>
        /// <value>The audio images data path.</value>
        public string AudioImagesDataPath
        {
            get
            {
                return Path.Combine(_appPaths.DataPath, "extracted-audio-images");
            }
        }

        /// <summary>
        /// Gets the subtitle cache path.
        /// </summary>
        /// <value>The subtitle cache path.</value>
        public string SubtitleCachePath
        {
            get
            {
                return Path.Combine(_appPaths.CachePath, "subtitles");
            }
        }

        /// <summary>
        /// The first chapter ticks
        /// </summary>
        private static readonly long FirstChapterTicks = TimeSpan.FromSeconds(15).Ticks;

        /// <summary>
        /// Extracts the chapter images.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="chapters">The chapters.</param>
        /// <param name="extractImages">if set to <c>true</c> [extract images].</param>
        /// <param name="saveChapters">if set to <c>true</c> [save chapters].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<bool> PopulateChapterImages(Video video, List<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken)
        {
            // Can't extract images if there are no video streams
            if (!video.DefaultVideoStreamIndex.HasValue)
            {
                return true;
            }

            var success = true;
            var changesMade = false;

            var runtimeTicks = video.RunTimeTicks ?? 0;

            foreach (var chapter in chapters)
            {
                if (chapter.StartPositionTicks >= runtimeTicks)
                {
                    _logger.Info("Stopping chapter extraction for {0} because a chapter was found with a position greater than the runtime.", video.Name);
                    break;
                }

                var filename = video.Path + "_" + video.DateModified.Ticks + "_" + chapter.StartPositionTicks;

                var path = VideoImageCache.GetResourcePath(filename, ".jpg");

                if (!File.Exists(path))
                {
                    if (extractImages)
                    {
                        if (video.VideoType == VideoType.HdDvd || video.VideoType == VideoType.Iso)
                        {
                            continue;
                        }

                        if (video.VideoType == VideoType.BluRay)
                        {
                            // Can only extract reliably on single file blurays
                            if (video.PlayableStreamFileNames == null || video.PlayableStreamFileNames.Count != 1)
                            {
                                continue;
                            }
                        }

                        // Add some time for the first chapter to make sure we don't end up with a black image
                        var time = chapter.StartPositionTicks == 0 ? TimeSpan.FromTicks(Math.Min(FirstChapterTicks, video.RunTimeTicks ?? 0)) : TimeSpan.FromTicks(chapter.StartPositionTicks);

                        InputType type;

                        var inputPath = MediaEncoderHelpers.GetInputArgument(video, null, out type);

                        try
                        {
                            var parentPath = Path.GetDirectoryName(path);

                            Directory.CreateDirectory(parentPath);

                            await _encoder.ExtractImage(inputPath, type, video.Video3DFormat, time, path, cancellationToken).ConfigureAwait(false);
                            chapter.ImagePath = path;
                            changesMade = true;
                        }
                        catch
                        {
                            success = false;
                            break;
                        }
                    }
                }
                else if (!string.Equals(path, chapter.ImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    chapter.ImagePath = path;
                    changesMade = true;
                }
            }

            if (saveChapters && changesMade)
            {
                await _itemRepo.SaveChapters(video.Id, chapters, cancellationToken).ConfigureAwait(false);
            }

            return success;
        }

        /// <summary>
        /// Gets the subtitle cache path.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="subtitleStreamIndex">Index of the subtitle stream.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="outputExtension">The output extension.</param>
        /// <returns>System.String.</returns>
        public string GetSubtitleCachePath(Video input, int subtitleStreamIndex, TimeSpan? offset, string outputExtension)
        {
            var ticksParam = offset.HasValue ? "_" + offset.Value.Ticks : "";

            var stream = _itemRepo.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = input.Id,
                Index = subtitleStreamIndex

            }).FirstOrDefault();

            if (stream == null)
            {
                return null;
            }

            if (stream.IsExternal)
            {
                ticksParam += _fileSystem.GetLastWriteTimeUtc(stream.Path).Ticks;
            }

            return SubtitleCache.GetResourcePath(input.Id + "_" + subtitleStreamIndex + "_" + input.DateModified.Ticks + ticksParam, outputExtension);
        }
    }
}
