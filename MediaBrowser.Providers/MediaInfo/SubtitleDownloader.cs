#nullable disable

#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleDownloader
    {
        private readonly ILogger _logger;
        private readonly ISubtitleManager _subtitleManager;

        public SubtitleDownloader(ILogger logger, ISubtitleManager subtitleManager)
        {
            _logger = logger;
            _subtitleManager = subtitleManager;
        }

        public async Task<List<string>> DownloadSubtitles(
            Video video,
            IReadOnlyList<MediaStream> mediaStreams,
            bool skipIfEmbeddedSubtitlesPresent,
            bool skipIfAudioTrackMatches,
            bool requirePerfectMatch,
            IEnumerable<string> languages,
            string[] disabledSubtitleFetchers,
            string[] subtitleFetcherOrder,
            bool isAutomated,
            CancellationToken cancellationToken)
        {
            var downloadedLanguages = new List<string>();

            foreach (var lang in languages)
            {
                var downloaded = await DownloadSubtitles(
                    video,
                    mediaStreams,
                    skipIfEmbeddedSubtitlesPresent,
                    skipIfAudioTrackMatches,
                    requirePerfectMatch,
                    lang,
                    disabledSubtitleFetchers,
                    subtitleFetcherOrder,
                    isAutomated,
                    cancellationToken).ConfigureAwait(false);

                if (downloaded)
                {
                    downloadedLanguages.Add(lang);
                }
            }

            return downloadedLanguages;
        }

        public Task<bool> DownloadSubtitles(
            Video video,
            IReadOnlyList<MediaStream> mediaStreams,
            bool skipIfEmbeddedSubtitlesPresent,
            bool skipIfAudioTrackMatches,
            bool requirePerfectMatch,
            string lang,
            string[] disabledSubtitleFetchers,
            string[] subtitleFetcherOrder,
            bool isAutomated,
            CancellationToken cancellationToken)
        {
            if (video.VideoType != VideoType.VideoFile)
            {
                return Task.FromResult(false);
            }

            if (!video.IsCompleteMedia)
            {
                return Task.FromResult(false);
            }

            VideoContentType mediaType;

            if (video is Episode)
            {
                mediaType = VideoContentType.Episode;
            }
            else if (video is Movie)
            {
                mediaType = VideoContentType.Movie;
            }
            else
            {
                // These are the only supported types
                return Task.FromResult(false);
            }

            return DownloadSubtitles(
                video,
                mediaStreams,
                skipIfEmbeddedSubtitlesPresent,
                skipIfAudioTrackMatches,
                requirePerfectMatch,
                lang,
                disabledSubtitleFetchers,
                subtitleFetcherOrder,
                mediaType,
                isAutomated,
                cancellationToken);
        }

        private async Task<bool> DownloadSubtitles(
            Video video,
            IReadOnlyList<MediaStream> mediaStreams,
            bool skipIfEmbeddedSubtitlesPresent,
            bool skipIfAudioTrackMatches,
            bool requirePerfectMatch,
            string language,
            string[] disabledSubtitleFetchers,
            string[] subtitleFetcherOrder,
            VideoContentType mediaType,
            bool isAutomated,
            CancellationToken cancellationToken)
        {
            // There's already subtitles for this language
            if (mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && i.IsTextSubtitleStream && string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var audioStreams = mediaStreams.Where(i => i.Type == MediaStreamType.Audio).ToList();
            var defaultAudioStreams = audioStreams.Where(i => i.IsDefault).ToList();

            // If none are marked as default, just take a guess
            if (defaultAudioStreams.Count == 0)
            {
                defaultAudioStreams = audioStreams.Take(1).ToList();
            }

            // There's already a default audio stream for this language
            if (skipIfAudioTrackMatches &&
                defaultAudioStreams.Any(i => string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // There's an internal subtitle stream for this language
            if (skipIfEmbeddedSubtitlesPresent &&
                mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal && string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // For automated runs, skip before contacting any provider when an external
            // subtitle file for this language already exists on disk next to the video
            // (or in its metadata folder). SearchSubtitles consumes provider quota and
            // GetSubtitles downloads the full content, so this check must happen before
            // either call — checking only after the download (as TrySaveSubtitle does)
            // still spends the rate limit on every scheduled task run.
            if (isAutomated && HasExternalSubtitleFile(video, language))
            {
                _logger.LogInformation("External {Language} subtitle already exists for {Path}, skipping provider search", language, video.Path);
                return false;
            }

            var request = new SubtitleSearchRequest
            {
                ContentType = mediaType,
                IndexNumber = video.IndexNumber,
                Language = language,
                MediaPath = video.Path,
                Name = video.Name,
                ParentIndexNumber = video.ParentIndexNumber,
                ProductionYear = video.ProductionYear,
                ProviderIds = video.ProviderIds,

                // Stop as soon as we find something
                SearchAllProviders = false,

                IsPerfectMatch = requirePerfectMatch,
                DisabledSubtitleFetchers = disabledSubtitleFetchers,
                SubtitleFetcherOrder = subtitleFetcherOrder,
                IsAutomated = isAutomated
            };

            if (video is Episode episode)
            {
                request.IndexNumberEnd = episode.IndexNumberEnd;
                request.SeriesName = episode.SeriesName;
            }

            try
            {
                var searchResults = await _subtitleManager.SearchSubtitles(request, cancellationToken).ConfigureAwait(false);

                var result = searchResults.FirstOrDefault();

                if (result is not null)
                {
                    await _subtitleManager.DownloadSubtitles(video, result.Id, cancellationToken).ConfigureAwait(false);

                    return true;
                }
            }
            catch (RateLimitExceededException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading subtitles");
            }

            return false;
        }

        private static bool HasExternalSubtitleFile(Video video, string language)
        {
            // Mirrors the naming TrySaveSubtitle uses: <video>.<language>[.forced][.sdh].<ext>,
            // plus the <video>.<language>.<n>.<ext> counter files created on name collisions.
            if (string.IsNullOrEmpty(video.Path))
            {
                return false;
            }

            var extensions = new[] { "srt", "ass", "ssa", "sub", "vtt", "txt" };
            var directories = new[] { video.ContainingFolderPath, video.GetInternalMetadataPath() };
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(video.Path);

            foreach (var directory in directories)
            {
                if (string.IsNullOrEmpty(directory))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(directory, fileNameWithoutExtension + ".*"))
                {
                    var rest = Path.GetFileName(file).Substring(fileNameWithoutExtension.Length + 1);
                    var segments = rest.Split('.');

                    // <language>.<ext> at minimum
                    if (segments.Length < 2)
                    {
                        continue;
                    }

                    if (!string.Equals(segments[0], language, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!extensions.Contains(segments[^1], StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Middle segments may only be forced/sdh markers or collision counters
                    var middleValid = true;
                    foreach (var segment in segments[1..^1])
                    {
                        if (string.Equals(segment, "forced", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(segment, "sdh", StringComparison.OrdinalIgnoreCase)
                            || int.TryParse(segment, out _))
                        {
                            continue;
                        }

                        middleValid = false;
                        break;
                    }

                    if (middleValid)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
