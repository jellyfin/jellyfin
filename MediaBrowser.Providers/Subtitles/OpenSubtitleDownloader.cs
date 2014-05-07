using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
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

        public OpenSubtitleDownloader(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
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

        public Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            return GetSubtitlesInternal(id, cancellationToken);
        }

        private async Task<SubtitleResponse> GetSubtitlesInternal(string id,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            var idParts = id.Split(new[] { '-' }, 3);

            var format = idParts[0];
            var language = idParts[1];
            var ossId = idParts[2];

            var downloadsList = new[] { int.Parse(ossId, _usCulture) };

            var resultDownLoad = OpenSubtitles.DownloadSubtitles(downloadsList);
            if (!(resultDownLoad is MethodResponseSubtitleDownload))
            {
                throw new ApplicationException("Invalid response type");
            }

            var res = ((MethodResponseSubtitleDownload)resultDownLoad).Results.First();
            var data = Convert.FromBase64String(res.Data);

            return new SubtitleResponse
            {
                Format = format,
                Language = language,

                Stream = new MemoryStream(Utilities.Decompress(new MemoryStream(data)))
            };
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            var imdbIdText = request.GetProviderId(MetadataProviders.Imdb);
            long imdbId;

            if (string.IsNullOrWhiteSpace(imdbIdText) ||
                !long.TryParse(imdbIdText.TrimStart('t'), NumberStyles.Any, _usCulture, out imdbId))
            {
                _logger.Debug("Imdb id missing");
                return new List<RemoteSubtitleInfo>();
            }

            switch (request.ContentType)
            {
                case SubtitleMediaType.Episode:
                    if (!request.IndexNumber.HasValue || !request.ParentIndexNumber.HasValue || string.IsNullOrEmpty(request.SeriesName))
                    {
                        _logger.Debug("Episode information missing");
                        return new List<RemoteSubtitleInfo>();
                    }
                    break;
                case SubtitleMediaType.Movie:
                    if (string.IsNullOrEmpty(request.Name))
                    {
                        _logger.Debug("Movie name missing");
                        return new List<RemoteSubtitleInfo>();
                    }
                    break;
            }

            if (string.IsNullOrEmpty(request.MediaPath))
            {
                _logger.Debug("Path Missing");
                return new List<RemoteSubtitleInfo>();
            }

            Utilities.HttpClient = _httpClient;
            OpenSubtitles.SetUserAgent("OS Test User Agent");

            var loginResponse = await OpenSubtitles.LogInAsync("", "", "en", cancellationToken).ConfigureAwait(false);

            if (!(loginResponse is MethodResponseLogIn))
            {
                _logger.Debug("Login error");
                return new List<RemoteSubtitleInfo>();
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
                _logger.Debug("Invalid response type");
                return new List<RemoteSubtitleInfo>();
            }

            Predicate<SubtitleSearchResult> mediaFilter =
                x =>
                    request.ContentType == SubtitleMediaType.Episode
                        ? int.Parse(x.SeriesSeason, _usCulture) == request.ParentIndexNumber && int.Parse(x.SeriesEpisode, _usCulture) == request.IndexNumber
                        : long.Parse(x.IDMovieImdb, _usCulture) == imdbId;

            var results = ((MethodResponseSubtitleSearch)result).Results;

            // Avoid implicitly captured closure
            var hasCopy = hash;

            return results.Where(x => x.SubBad == "0" && mediaFilter(x))
                    .OrderBy(x => x.MovieHash == hash)
                    .ThenBy(x => Math.Abs(long.Parse(x.MovieByteSize, _usCulture) - movieByteSize))
                    .ThenByDescending(x => int.Parse(x.SubDownloadsCnt, _usCulture))
                    .ThenByDescending(x => double.Parse(x.SubRating, _usCulture))
                    .Select(i => new RemoteSubtitleInfo
                    {
                        Author = i.UserNickName,
                        Comment = i.SubAuthorComment,
                        CommunityRating = float.Parse(i.SubRating, _usCulture),
                        DownloadCount = int.Parse(i.SubDownloadsCnt, _usCulture),
                        Format = i.SubFormat,
                        ProviderName = Name,
                        Language = i.SubLanguageID,

                        Id = i.SubFormat + "-" + i.SubLanguageID + "-" + i.IDSubtitle,

                        Name = i.SubFileName,
                        DateCreated = DateTime.Parse(i.SubAddDate, _usCulture),
                        IsHashMatch = i.MovieHash == hasCopy
                    });
        }
    }
}
