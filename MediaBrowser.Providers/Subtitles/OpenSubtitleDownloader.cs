using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using OpenSubtitlesHandler;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Subtitles
{
    public class OpenSubtitleDownloader : ISubtitleProvider
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public OpenSubtitleDownloader(ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public string Name
        {
            get { return "Open Subtitles"; }
        }

        public IEnumerable<SubtitleMediaType> SupportedMediaTypes
        {
            get { return new[] { SubtitleMediaType.Episode, SubtitleMediaType.Movie }; }
        }

        public Task<SubtitleResponse> GetSubtitles(SubtitleRequest request, CancellationToken cancellationToken)
        {
            return GetSubtitlesInternal(request, cancellationToken);
        }

        private async Task<SubtitleResponse> GetSubtitlesInternal(SubtitleRequest request, 
            CancellationToken cancellationToken)
        {
            var response = new SubtitleResponse();

            var imdbIdText = request.GetProviderId(MetadataProviders.Imdb);
            long imdbId;

            if (string.IsNullOrWhiteSpace(imdbIdText) ||
                long.TryParse(imdbIdText.TrimStart('t'), NumberStyles.Any, _usCulture, out imdbId))
            {
                return response;
            }
            
            switch (request.ContentType)
            {
                case SubtitleMediaType.Episode:
                    if (!request.IndexNumber.HasValue || !request.ParentIndexNumber.HasValue || string.IsNullOrEmpty(request.SeriesName))
                    {
                        _logger.Debug("Information Missing");
                        return response;
                    }
                    break;
                case SubtitleMediaType.Movie:
                    if (string.IsNullOrEmpty(request.Name))
                    {
                        _logger.Debug("Information Missing");
                        return response;
                    }
                    break;
            }

            if (string.IsNullOrEmpty(request.MediaPath))
            {
                _logger.Debug("Path Missing");
                return response;
            }

            Utilities.HttpClient = _httpClient;
            OpenSubtitles.SetUserAgent("OS Test User Agent");
            var loginResponse = OpenSubtitles.LogIn("", "", "en");
            if (!(loginResponse is MethodResponseLogIn))
            {
                _logger.Debug("Login error");
                return response;
            }

            var subLanguageId = request.Language;
            var hash = Utilities.ComputeHash(request.MediaPath);
            var fileInfo = new FileInfo(request.MediaPath);
            var movieByteSize = fileInfo.Length;

            var subtitleSearchParameters = request.ContentType == SubtitleMediaType.Episode
                ? new SubtitleSearchParameters(subLanguageId, request.SeriesName, request.ParentIndexNumber.Value.ToString(_usCulture), request.IndexNumber.Value.ToString(_usCulture))
                : new SubtitleSearchParameters(subLanguageId, request.Name);

            var parms = new List<SubtitleSearchParameters> {
                                                               new SubtitleSearchParameters(subLanguageId, hash, movieByteSize),
                                                               subtitleSearchParameters
                                                           };

            var result = OpenSubtitles.SearchSubtitles(parms.ToArray());
            if (!(result is MethodResponseSubtitleSearch))
            {
                _logger.Debug("invalid response type");
                return null;
            }

            Predicate<SubtitleSearchResult> mediaFilter =
                x =>
                    request.ContentType == SubtitleMediaType.Episode
                        ? int.Parse(x.SeriesSeason) == request.ParentIndexNumber && int.Parse(x.SeriesEpisode) == request.IndexNumber
                        : long.Parse(x.IDMovieImdb) == imdbId;

            var results = ((MethodResponseSubtitleSearch)result).Results;
            var bestResult = results.Where(x => x.SubBad == "0" && mediaFilter(x))
                    .OrderBy(x => x.MovieHash == hash)
                    .ThenBy(x => Math.Abs(long.Parse(x.MovieByteSize) - movieByteSize))
                    .ThenByDescending(x => int.Parse(x.SubDownloadsCnt))
                    .ThenByDescending(x => double.Parse(x.SubRating))
                    .ToList();

            if (!bestResult.Any())
            {
                _logger.Debug("No Subtitles");
                return response;
            }

            _logger.Debug("Found " + bestResult.Count + " subtitles.");

            var subtitle = bestResult.First();
            var downloadsList = new[] { int.Parse(subtitle.IDSubtitleFile) };

            var resultDownLoad = OpenSubtitles.DownloadSubtitles(downloadsList);
            if (!(resultDownLoad is MethodResponseSubtitleDownload))
            {
                _logger.Debug("invalid response type");
                return response;
            }
            if (!((MethodResponseSubtitleDownload)resultDownLoad).Results.Any())
            {
                _logger.Debug("No Subtitle Downloads");
                return response;
            }

            var res = ((MethodResponseSubtitleDownload)resultDownLoad).Results.First();
            var data = Convert.FromBase64String(res.Data);

            response.HasContent = true;
            response.Format = subtitle.SubFormat.ToUpper();
            response.Stream = new MemoryStream(Utilities.Decompress(new MemoryStream(data)));
            return response;
        }
    }
}
