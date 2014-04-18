using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.MediaEncoding;
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

namespace MediaBrowser.Server.Implementations.MediaEncoder
{
    public class EncodingManager : IEncodingManager
    {
        private readonly IServerConfigurationManager _config;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;
        private readonly IMediaEncoder _encoder;

        public EncodingManager(IServerConfigurationManager config, IFileSystem fileSystem, ILogger logger, IItemRepository itemRepo, IMediaEncoder encoder)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
            _itemRepo = itemRepo;
            _encoder = encoder;
        }

        private string SubtitleCachePath
        {
            get
            {
                return Path.Combine(_config.ApplicationPaths.CachePath, "subtitles");
            }
        }
        
        public string GetSubtitleCachePath(string originalSubtitlePath, string outputSubtitleExtension)
        {
            var ticksParam = _fileSystem.GetLastWriteTimeUtc(originalSubtitlePath).Ticks;

            var filename = (originalSubtitlePath + ticksParam).GetMD5() + outputSubtitleExtension;

            var prefix = filename.Substring(0, 1);

            return Path.Combine(SubtitleCachePath, prefix, filename);
        }

        public string GetSubtitleCachePath(string mediaPath, int subtitleStreamIndex, string outputSubtitleExtension)
        {
            var ticksParam = string.Empty;

            var date = _fileSystem.GetLastWriteTimeUtc(mediaPath);

            var filename = (mediaPath + "_" + subtitleStreamIndex.ToString(_usCulture) + "_" + date.Ticks.ToString(_usCulture) + ticksParam).GetMD5() + outputSubtitleExtension;

            var prefix = filename.Substring(0, 1);

            return Path.Combine(SubtitleCachePath, prefix, filename);
        }

        /// <summary>
        /// Gets the chapter images data path.
        /// </summary>
        /// <value>The chapter images data path.</value>
        private string GetChapterImagesPath(Guid itemId)
        {
            return Path.Combine(_config.ApplicationPaths.GetInternalMetadataPath(itemId), "chapters");
        }

        /// <summary>
        /// Determines whether [is eligible for chapter image extraction] [the specified video].
        /// </summary>
        /// <param name="video">The video.</param>
        /// <returns><c>true</c> if [is eligible for chapter image extraction] [the specified video]; otherwise, <c>false</c>.</returns>
        private bool IsEligibleForChapterImageExtraction(Video video)
        {
            if (video.IsPlaceHolder)
            {
                return false;
            }

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

        public async Task<bool> RefreshChapterImages(ChapterImageRefreshOptions options, CancellationToken cancellationToken)
        {
            var extractImages = options.ExtractImages;
            var video = options.Video;
            var chapters = options.Chapters;
            var saveChapters = options.SaveChapters;

            if (!IsEligibleForChapterImageExtraction(video))
            {
                extractImages = false;
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

                        // Add some time for the first chapter to make sure we don't end up with a black image
                        var time = chapter.StartPositionTicks == 0 ? TimeSpan.FromTicks(Math.Min(FirstChapterTicks, video.RunTimeTicks ?? 0)) : TimeSpan.FromTicks(chapter.StartPositionTicks);

                        InputType type;

                        var inputPath = MediaEncoderHelpers.GetInputArgument(video.Path, false, video.VideoType, video.IsoType, null, video.PlayableStreamFileNames, out type);

                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path));

                            using (var stream = await _encoder.ExtractVideoImage(inputPath, type, video.Video3DFormat, time, cancellationToken).ConfigureAwait(false))
                            {
                                using (var fileStream = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                                {
                                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                                }
                            }

                            chapter.ImagePath = path;
                            changesMade = true;
                        }
                        catch
                        {
                            success = false;
                            break;
                        }
                    }
                    else if (!string.IsNullOrEmpty(chapter.ImagePath))
                    {
                        chapter.ImagePath = null;
                        changesMade = true;
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

        private string GetChapterImagePath(Video video, long chapterPositionTicks)
        {
            var filename = video.DateModified.Ticks.ToString(_usCulture) + "_" + chapterPositionTicks.ToString(_usCulture) + ".jpg";

            return Path.Combine(GetChapterImagesPath(video.Id), filename);
        }

        private List<string> GetSavedChapterImages(Video video)
        {
            var path = GetChapterImagesPath(video.Id);

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
    }
}
