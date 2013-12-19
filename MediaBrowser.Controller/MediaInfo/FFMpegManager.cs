using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly IServerConfigurationManager _config;
        private readonly IMediaEncoder _encoder;
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;

        private readonly IFileSystem _fileSystem;

        public static FFMpegManager Instance { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FFMpegManager" /> class.
        /// </summary>
        /// <param name="encoder">The encoder.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="itemRepo">The item repo.</param>
        /// <exception cref="System.ArgumentNullException">zipClient</exception>
        public FFMpegManager(IMediaEncoder encoder, ILogger logger, IItemRepository itemRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _encoder = encoder;
            _logger = logger;
            _itemRepo = itemRepo;
            _fileSystem = fileSystem;
            _config = config;

            // TODO: Remove this static instance
            Instance = this;
        }

        /// <summary>
        /// Gets the chapter images data path.
        /// </summary>
        /// <value>The chapter images data path.</value>
        public string ChapterImagesPath
        {
            get
            {
                return Path.Combine(_config.ApplicationPaths.DataPath, "chapter-images");
            }
        }

        /// <summary>
        /// Gets the subtitle cache path.
        /// </summary>
        /// <value>The subtitle cache path.</value>
        private string SubtitleCachePath
        {
            get
            {
                return Path.Combine(_config.ApplicationPaths.CachePath, "subtitles");
            }
        }

        /// <summary>
        /// Determines whether [is eligible for chapter image extraction] [the specified video].
        /// </summary>
        /// <param name="video">The video.</param>
        /// <returns><c>true</c> if [is eligible for chapter image extraction] [the specified video]; otherwise, <c>false</c>.</returns>
        private bool IsEligibleForChapterImageExtraction(Video video)
        {
            if (video is Movie)
            {
                if (!_config.Configuration.EnableMovieChapterImageExtraction)
                {
                    return false;
                }
            }
            else if (video is Episode)
            {
                if (!_config.Configuration.EnableEpisodeChapterImageExtraction)
                {
                    return false;
                }
            }
            else 
            {
                if (!_config.Configuration.EnableOtherVideoChapterImageExtraction)
                {
                    return false;
                }
            }

            // Can't extract images if there are no video streams
            return video.DefaultVideoStreamIndex.HasValue;
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
            if (!IsEligibleForChapterImageExtraction(video))
            {
                return true;
            }

            var success = true;
            var changesMade = false;

            var runtimeTicks = video.RunTimeTicks ?? 0;

            var currentImages = GetSavedChapterImages(video);

            foreach (var chapter in chapters)
            {
                if (chapter.StartPositionTicks >= runtimeTicks)
                {
                    _logger.Info("Stopping chapter extraction for {0} because a chapter was found with a position greater than the runtime.", video.Name);
                    break;
                }

                var path = GetChapterImagePath(video, chapter.StartPositionTicks);

                if (!currentImages.Contains(path, StringComparer.OrdinalIgnoreCase))
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

                        var inputPath = MediaEncoderHelpers.GetInputArgument(video.Path, false, video.VideoType, video.IsoType, null, video.PlayableStreamFileNames, out type);

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

            DeleteDeadImages(currentImages, chapters);

            return success;
        }

        private void DeleteDeadImages(IEnumerable<string> images, IEnumerable<ChapterInfo> chapters)
        {
            var deadImages = images
                .Except(chapters.Select(i => i.ImagePath).Where(i => !string.IsNullOrEmpty(i)), StringComparer.OrdinalIgnoreCase)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(Path.GetExtension(i), StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var image in deadImages)
            {
                _logger.Debug("Deleting dead chapter image {0}", image);

                try
                {
                    File.Delete(image);
                }
                catch (IOException ex)
                {
                    _logger.ErrorException("Error deleting {0}.", ex, image);
                }
            }
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the subtitle cache path.
        /// </summary>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="outputExtension">The output extension.</param>
        /// <returns>System.String.</returns>
        public string GetSubtitleCachePath(string mediaPath, MediaStream subtitleStream, TimeSpan? offset, string outputExtension)
        {
            var ticksParam = offset.HasValue ? "_" + offset.Value.Ticks : "";

            if (subtitleStream.IsExternal)
            {
                ticksParam += _fileSystem.GetLastWriteTimeUtc(subtitleStream.Path).Ticks;
            }

            var date = _fileSystem.GetLastWriteTimeUtc(mediaPath);

            var filename = (mediaPath + "_" + subtitleStream.Index.ToString(_usCulture) + "_" + date.Ticks.ToString(_usCulture) + ticksParam).GetMD5() + outputExtension;

            var prefix = filename.Substring(0, 1);

            return Path.Combine(SubtitleCachePath, prefix, filename);
        }

        public string GetChapterImagePath(Video video, long chapterPositionTicks)
        {
            var filename = video.DateModified.Ticks.ToString(_usCulture) + "_" + chapterPositionTicks.ToString(_usCulture) + ".jpg";

            var videoId = video.Id.ToString();
            var prefix = videoId.Substring(0, 1);

            return Path.Combine(ChapterImagesPath, prefix, videoId, filename);
        }

        public List<string> GetSavedChapterImages(Video video)
        {
            var videoId = video.Id.ToString();
            var prefix = videoId.Substring(0, 1);

            var path = Path.Combine(ChapterImagesPath, prefix, videoId);

            try
            {
                return Directory.EnumerateFiles(path)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<string>();
            }
        }
    }
}
