using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
            List<MediaStream> internalMediaStreams,
            List<MediaStream> externalSubtitleStreams,
            bool skipIfGraphicalSubtitlesPresent,
            bool skipIfAudioTrackMatches,
            IEnumerable<string> languages,
            CancellationToken cancellationToken)
        {
            if (video.LocationType != LocationType.FileSystem ||
                video.VideoType != VideoType.VideoFile)
            {
                return new List<string>();
            }

            SubtitleMediaType mediaType;

            if (video is Episode)
            {
                mediaType = SubtitleMediaType.Episode;
            }
            else if (video is Movie)
            {
                mediaType = SubtitleMediaType.Movie;
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
                    var downloaded = await DownloadSubtitles(video, internalMediaStreams, externalSubtitleStreams, skipIfGraphicalSubtitlesPresent, skipIfAudioTrackMatches, lang, mediaType, cancellationToken)
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
            List<MediaStream> internalMediaStreams,
            IEnumerable<MediaStream> externalSubtitleStreams,
            bool skipIfGraphicalSubtitlesPresent,
            bool skipIfAudioTrackMatches,
            string language,
            SubtitleMediaType mediaType,
            CancellationToken cancellationToken)
        {
            // There's already subtitles for this language
            if (externalSubtitleStreams.Any(i => string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // There's already an audio stream for this language
            if (skipIfAudioTrackMatches &&
                internalMediaStreams.Any(i => i.Type == MediaStreamType.Audio && string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // There's an internal subtitle stream for this language
            if (skipIfGraphicalSubtitlesPresent &&
                internalMediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
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
                ProviderIds = video.ProviderIds
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
                    await _subtitleManager.DownloadSubtitles(video, result.Id, result.ProviderName, cancellationToken)
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
