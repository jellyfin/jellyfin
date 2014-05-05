using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
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
            return request.ContentType == SubtitleMediaType.Episode
                ? GetEpisodeSubtitles(request, cancellationToken)
                : GetMovieSubtitles(request, cancellationToken);
        }

        public async Task<SubtitleResponse> GetMovieSubtitles(SubtitleRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<SubtitleResponse> GetEpisodeSubtitles(SubtitleRequest request, CancellationToken cancellationToken)
        {
            var response = new SubtitleResponse();

            if (!request.IndexNumber.HasValue || !request.ParentIndexNumber.HasValue)
            {
                _logger.Debug("Information Missing");
                return response;
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

            var parms = new List<SubtitleSearchParameters> {
                                                               new SubtitleSearchParameters(subLanguageId, hash, movieByteSize),
                                                               new SubtitleSearchParameters(subLanguageId, request.SeriesName, request.ParentIndexNumber.Value.ToString(_usCulture), request.IndexNumber.Value.ToString(_usCulture)),

                                                           };

            var result = OpenSubtitles.SearchSubtitles(parms.ToArray());
            if (!(result is MethodResponseSubtitleSearch))
            {
                _logger.Debug("invalid response type");
                return null;
            }

            var results = ((MethodResponseSubtitleSearch)result).Results;
            var bestResult = results.Where(x => x.SubBad == "0" && int.Parse(x.SeriesSeason) == request.ParentIndexNumber && int.Parse(x.SeriesEpisode) == request.IndexNumber)
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
            response.Format = SubtitleFormat.SRT;
            response.Stream = new MemoryStream(Utilities.Decompress(new MemoryStream(data)));
            return response;
        }
    }
}
