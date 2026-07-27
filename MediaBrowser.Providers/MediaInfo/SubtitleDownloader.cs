#nullable disable

#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
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
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleDownloader
    {
        private readonly ILogger _logger;
        private readonly ISubtitleManager _subtitleManager;
        private readonly ILocalizationManager _localization;

        public SubtitleDownloader(ILogger logger, ISubtitleManager subtitleManager, ILocalizationManager localization)
        {
            _logger = logger;
            _subtitleManager = subtitleManager;
            _localization = localization;
        }

        /// <summary>
        /// Normalizes an ISO language code (2-letter, 3-letter, or full name) to the
        /// canonical <see cref="CultureDto.Name"/> so two codes referring to the same
        /// language compare equal. Returns the input unchanged when no culture matches.
        /// </summary>
        /// <remarks>
        /// Media streams store the language detected by FFprobe via <c>ExternalPathParser</c>,
        /// which prefers the ISO 639-3 code (e.g. "eng"), while library options and subtitle
        /// requests commonly use ISO 639-2 codes (e.g. "en"). A plain ordinal compare lets
        /// an existing subtitle slip through the pre-download guard, causing a redundant
        /// download that consumes the OpenSubtitles daily quota and re-creates the file.
        /// </remarks>
        private string NormalizeLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return string.Empty;
            }

            var culture = _localization.FindLanguageInfo(language);
            return culture?.Name ?? language;
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
            // Normalize the requested language so a 2-letter/3-letter code mismatch
            // (e.g. requested "en" vs. an existing stream stored as "eng") doesn't
            // bypass these guards and trigger a redundant download.
            var normalizedLanguage = NormalizeLanguage(language);

            // There's already subtitles for this language
            if (mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && i.IsTextSubtitleStream && string.Equals(NormalizeLanguage(i.Language), normalizedLanguage, StringComparison.OrdinalIgnoreCase)))
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
                defaultAudioStreams.Any(i => string.Equals(NormalizeLanguage(i.Language), normalizedLanguage, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // There's an internal subtitle stream for this language
            if (skipIfEmbeddedSubtitlesPresent &&
                mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal && string.Equals(NormalizeLanguage(i.Language), normalizedLanguage, StringComparison.OrdinalIgnoreCase)))
            {
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
    }
}
