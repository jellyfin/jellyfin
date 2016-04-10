using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<List<string>> DownloadSubtitles(Video video,
            List<MediaStream> mediaStreams,
            bool skipIfEmbeddedSubtitlesPresent,
            bool skipIfAudioTrackMatches,
            bool requirePerfectMatch,
            IEnumerable<string> languages,
            CancellationToken cancellationToken)
        {
            if (video.LocationType != LocationType.FileSystem ||
                video.VideoType != VideoType.VideoFile)
            {
                return new List<string>();
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
                return new List<string>();
            }

            var downloadedLanguages = new List<string>();

            foreach (var lang in languages)
            {
                try
                {
                    var downloaded = await DownloadSubtitles(video, mediaStreams, skipIfEmbeddedSubtitlesPresent, skipIfAudioTrackMatches, requirePerfectMatch, lang, mediaType, cancellationToken)
                        .ConfigureAwait(false);

                    if (downloaded)
                    {
                        downloadedLanguages.Add(lang);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading subtitles", ex);
                }
            }

            return downloadedLanguages;
        }

        private async Task<bool> DownloadSubtitles(Video video,
            List<MediaStream> mediaStreams,
            bool skipIfEmbeddedSubtitlesPresent,
            bool skipIfAudioTrackMatches,
            bool requirePerfectMatch,
            string language,
            VideoContentType mediaType,
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

                IsPerfectMatch = requirePerfectMatch
            };

            var episode = video as Episode;

            if (episode != null)
            {
                request.IndexNumberEnd = episode.IndexNumberEnd;
                request.SeriesName = episode.SeriesName;
            }

            try
            {
                var searchResults = await _subtitleManager.SearchSubtitles(request, cancellationToken).ConfigureAwait(false);

                var result = searchResults.FirstOrDefault();

                if (result != null)
                {
                    await _subtitleManager.DownloadSubtitles(video, result.Id, cancellationToken)
                            .ConfigureAwait(false);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error downloading subtitles", ex);
            }

            return false;
        }
    }
}
