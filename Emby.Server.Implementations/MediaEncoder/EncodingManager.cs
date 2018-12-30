using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

namespace Emby.Server.Implementations.MediaEncoder
{
    public class EncodingManager : IEncodingManager
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IMediaEncoder _encoder;
        private readonly IChapterManager _chapterManager;
        private readonly ILibraryManager _libraryManager;

        public EncodingManager(IFileSystem fileSystem,
            ILogger logger,
            IMediaEncoder encoder,
            IChapterManager chapterManager, ILibraryManager libraryManager)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _encoder = encoder;
            _chapterManager = chapterManager;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the chapter images data path.
        /// </summary>
        /// <value>The chapter images data path.</value>
        private string GetChapterImagesPath(BaseItem item)
        {
            return Path.Combine(item.GetInternalMetadataPath(), "chapters");
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

            var libraryOptions = _libraryManager.GetLibraryOptions(video);
            if (libraryOptions != null)
            {
                if (!libraryOptions.EnableChapterImageExtraction)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (video.VideoType == VideoType.Iso)
            {
                return false;
            }

            if (video.VideoType == VideoType.BluRay || video.VideoType == VideoType.Dvd)
            {
                return false;
            }

            if (video.IsShortcut)
            {
                return false;
            }

            if (!video.IsCompleteMedia)
            {
                return false;
            }

            // Can't extract images if there are no video streams
            return video.DefaultVideoStreamIndex.HasValue;
        }

        /// <summary>
        /// The first chapter ticks
        /// </summary>
        private static readonly long FirstChapterTicks = TimeSpan.FromSeconds(15).Ticks;

        public async Task<bool> RefreshChapterImages(Video video, IDirectoryService directoryService, List<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken)
        {
            if (!IsEligibleForChapterImageExtraction(video))
            {
                extractImages = false;
            }

            var success = true;
            var changesMade = false;

            var runtimeTicks = video.RunTimeTicks ?? 0;

            var currentImages = GetSavedChapterImages(video, directoryService);

            foreach (var chapter in chapters)
            {
                if (chapter.StartPositionTicks >= runtimeTicks)
                {
                    _logger.LogInformation("Stopping chapter extraction for {0} because a chapter was found with a position greater than the runtime.", video.Name);
                    break;
                }

                var path = GetChapterImagePath(video, chapter.StartPositionTicks);

                if (!currentImages.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    if (extractImages)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            // Add some time for the first chapter to make sure we don't end up with a black image
                            var time = chapter.StartPositionTicks == 0 ? TimeSpan.FromTicks(Math.Min(FirstChapterTicks, video.RunTimeTicks ?? 0)) : TimeSpan.FromTicks(chapter.StartPositionTicks);

                            var protocol = MediaProtocol.File;

                            var inputPath = MediaEncoderHelpers.GetInputArgument(_fileSystem, video.Path, protocol, null, Array.Empty<string>());

                            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(path));

                            var container = video.Container;

                            var tempFile = await _encoder.ExtractVideoImage(inputPath, container, protocol, video.GetDefaultVideoStream(), video.Video3DFormat, time, cancellationToken).ConfigureAwait(false);
                            _fileSystem.CopyFile(tempFile, path, true);

                            try
                            {
                                _fileSystem.DeleteFile(tempFile);
                            }
                            catch
                            {

                            }

                            chapter.ImagePath = path;
                            chapter.ImageDateModified = _fileSystem.GetLastWriteTimeUtc(path);
                            changesMade = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error extracting chapter images for {0}", string.Join(",", video.Path));
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
                    chapter.ImageDateModified = _fileSystem.GetLastWriteTimeUtc(path);
                    changesMade = true;
                }
            }

            if (saveChapters && changesMade)
            {
                _chapterManager.SaveChapters(video.Id.ToString(), chapters);
            }

            DeleteDeadImages(currentImages, chapters);

            return success;
        }

        private string GetChapterImagePath(Video video, long chapterPositionTicks)
        {
            var filename = video.DateModified.Ticks.ToString(_usCulture) + "_" + chapterPositionTicks.ToString(_usCulture) + ".jpg";

            return Path.Combine(GetChapterImagesPath(video), filename);
        }

        private List<string> GetSavedChapterImages(Video video, IDirectoryService directoryService)
        {
            var path = GetChapterImagesPath(video);

            try
            {
                return directoryService.GetFilePaths(path)
                    .ToList();
            }
            catch (IOException)
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
                _logger.LogDebug("Deleting dead chapter image {path}", image);

                try
                {
                    _fileSystem.DeleteFile(image);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error deleting {path}.", image);
                }
            }
        }
    }
}
