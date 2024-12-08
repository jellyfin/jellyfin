#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.MediaEncoder
{
    public class EncodingManager : IEncodingManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<EncodingManager> _logger;
        private readonly IMediaEncoder _encoder;
        private readonly IChapterRepository _chapterManager;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The first chapter ticks.
        /// </summary>
        private static readonly long _firstChapterTicks = TimeSpan.FromSeconds(15).Ticks;

        public EncodingManager(
            ILogger<EncodingManager> logger,
            IFileSystem fileSystem,
            IMediaEncoder encoder,
            IChapterRepository chapterManager,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _encoder = encoder;
            _chapterManager = chapterManager;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the chapter images data path.
        /// </summary>
        /// <value>The chapter images data path.</value>
        private static string GetChapterImagesPath(BaseItem item)
        {
            return Path.Combine(item.GetInternalMetadataPath(), "chapters");
        }

        /// <summary>
        /// Determines whether [is eligible for chapter image extraction] [the specified video].
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="libraryOptions">The library options for the video.</param>
        /// <returns><c>true</c> if [is eligible for chapter image extraction] [the specified video]; otherwise, <c>false</c>.</returns>
        private bool IsEligibleForChapterImageExtraction(Video video, LibraryOptions libraryOptions)
        {
            if (video.IsPlaceHolder)
            {
                return false;
            }

            if (libraryOptions is null || !libraryOptions.EnableChapterImageExtraction)
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

        private long GetAverageDurationBetweenChapters(IReadOnlyList<ChapterInfo> chapters)
        {
            if (chapters.Count < 2)
            {
                return 0;
            }

            long sum = 0;
            for (int i = 1; i < chapters.Count; i++)
            {
                sum += chapters[i].StartPositionTicks - chapters[i - 1].StartPositionTicks;
            }

            return sum / chapters.Count;
        }

        public async Task<bool> RefreshChapterImages(Video video, IDirectoryService directoryService, IReadOnlyList<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken)
        {
            if (chapters.Count == 0)
            {
                return true;
            }

            var libraryOptions = _libraryManager.GetLibraryOptions(video);

            if (!IsEligibleForChapterImageExtraction(video, libraryOptions))
            {
                extractImages = false;
            }

            var averageChapterDuration = GetAverageDurationBetweenChapters(chapters);
            var threshold = TimeSpan.FromSeconds(1).Ticks;
            if (averageChapterDuration < threshold)
            {
                _logger.LogInformation("Skipping chapter image extraction for {Video} as the average chapter duration {AverageDuration} was lower than the minimum threshold {Threshold}", video.Name, averageChapterDuration, threshold);
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

                if (!currentImages.Contains(path, StringComparison.OrdinalIgnoreCase))
                {
                    if (extractImages)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            // Add some time for the first chapter to make sure we don't end up with a black image
                            var time = chapter.StartPositionTicks == 0 ? TimeSpan.FromTicks(Math.Min(_firstChapterTicks, video.RunTimeTicks ?? 0)) : TimeSpan.FromTicks(chapter.StartPositionTicks);

                            var inputPath = video.Path;

                            Directory.CreateDirectory(Path.GetDirectoryName(path));

                            var container = video.Container;
                            var mediaSource = new MediaSourceInfo
                            {
                                VideoType = video.VideoType,
                                IsoType = video.IsoType,
                                Protocol = video.PathProtocol.Value,
                            };

                            var tempFile = await _encoder.ExtractVideoImage(inputPath, container, mediaSource, video.GetDefaultVideoStream(), video.Video3DFormat, time, cancellationToken).ConfigureAwait(false);
                            File.Copy(tempFile, path, true);

                            try
                            {
                                _fileSystem.DeleteFile(tempFile);
                            }
                            catch (IOException ex)
                            {
                                _logger.LogError(ex, "Error deleting temporary chapter image encoding file {Path}", tempFile);
                            }

                            chapter.ImagePath = path;
                            chapter.ImageDateModified = _fileSystem.GetLastWriteTimeUtc(path);
                            changesMade = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error extracting chapter images for {0}", string.Join(',', video.Path));
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
                else if (libraryOptions?.EnableChapterImageExtraction != true)
                {
                    // We have an image for the current chapter but the user has disabled chapter image extraction -> delete this chapter's image
                    chapter.ImagePath = null;
                    changesMade = true;
                }
            }

            if (saveChapters && changesMade)
            {
                _chapterManager.SaveChapters(video.Id, chapters);
            }

            DeleteDeadImages(currentImages, chapters);

            return success;
        }

        private string GetChapterImagePath(Video video, long chapterPositionTicks)
        {
            var filename = video.DateModified.Ticks.ToString(CultureInfo.InvariantCulture) + "_" + chapterPositionTicks.ToString(CultureInfo.InvariantCulture) + ".jpg";

            return Path.Combine(GetChapterImagesPath(video), filename);
        }

        private static IReadOnlyList<string> GetSavedChapterImages(Video video, IDirectoryService directoryService)
        {
            var path = GetChapterImagesPath(video);
            if (!Directory.Exists(path))
            {
                return Array.Empty<string>();
            }

            try
            {
                return directoryService.GetFilePaths(path);
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
        }

        private void DeleteDeadImages(IEnumerable<string> images, IEnumerable<ChapterInfo> chapters)
        {
            var deadImages = images
                .Except(chapters.Select(i => i.ImagePath).Where(i => !string.IsNullOrEmpty(i)), StringComparer.OrdinalIgnoreCase)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(Path.GetExtension(i.AsSpan()), StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var image in deadImages)
            {
                _logger.LogDebug("Deleting dead chapter image {Path}", image);

                try
                {
                    _fileSystem.DeleteFile(image);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error deleting {Path}.", image);
                }
            }
        }
    }
}
