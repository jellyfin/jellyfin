using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
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
    public class OpenSubtitleDownloader : ISubtitleProvider, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IServerConfigurationManager _config;
        private readonly IEncryptionManager _encryption;

        public OpenSubtitleDownloader(ILogManager logManager, IHttpClient httpClient, IServerConfigurationManager config, IEncryptionManager encryption)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
            _config = config;
            _encryption = encryption;

            _config.ConfigurationUpdating += _config_ConfigurationUpdating;
        }

        private const string PasswordHashPrefix = "h:";
        void _config_ConfigurationUpdating(object sender, GenericEventArgs<ServerConfiguration> e)
        {
            var options = e.Argument.SubtitleOptions;

            if (options != null &&
                !string.IsNullOrWhiteSpace(options.OpenSubtitlesPasswordHash) &&
                !options.OpenSubtitlesPasswordHash.StartsWith(PasswordHashPrefix, StringComparison.OrdinalIgnoreCase))
            {
                options.OpenSubtitlesPasswordHash = EncryptPassword(options.OpenSubtitlesPasswordHash);
            }
        }

        private string EncryptPassword(string password)
        {
            return PasswordHashPrefix + _encryption.EncryptString(password);
        }

        private string DecryptPassword(string password)
        {
            if (password == null ||
                !password.StartsWith(PasswordHashPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return _encryption.DecryptString(password.Substring(2));
        }

        public string Name
        {
            get { return "Open Subtitles"; }
        }

        public IEnumerable<SubtitleMediaType> SupportedMediaTypes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_config.Configuration.SubtitleOptions.OpenSubtitlesUsername) ||
                    string.IsNullOrWhiteSpace(_config.Configuration.SubtitleOptions.OpenSubtitlesPasswordHash))
                {
                    return new SubtitleMediaType[] { };
                }

                return new[] { SubtitleMediaType.Episode, SubtitleMediaType.Movie };
            }
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

            await Login(cancellationToken).ConfigureAwait(false);

            var resultDownLoad = await OpenSubtitles.DownloadSubtitlesAsync(downloadsList, cancellationToken).ConfigureAwait(false);

            if (!(resultDownLoad is MethodResponseSubtitleDownload))
            {
                throw new ApplicationException("Invalid response type");
            }

            var results = ((MethodResponseSubtitleDownload)resultDownLoad).Results;

            if (results.Count == 0)
            {
                var msg = string.Format("Subtitle with Id {0} was not found. Name: {1}. Status: {2}. Message: {3}",
                    ossId,
                    resultDownLoad.Name ?? string.Empty,
                    resultDownLoad.Message ?? string.Empty,
                    resultDownLoad.Status ?? string.Empty);

                throw new ResourceNotFoundException(msg);
            }

            var data = Convert.FromBase64String(results.First().Data);

            return new SubtitleResponse
            {
                Format = format,
                Language = language,

                Stream = new MemoryStream(Utilities.Decompress(new MemoryStream(data)))
            };
        }

        private DateTime _lastLogin;
        private async Task Login(CancellationToken cancellationToken)
        {
            if ((DateTime.UtcNow - _lastLogin).TotalSeconds < 60)
            {
                return;
            }

            var options = _config.Configuration.SubtitleOptions ?? new SubtitleOptions();

            var user = options.OpenSubtitlesUsername ?? string.Empty;
            var password = DecryptPassword(options.OpenSubtitlesPasswordHash);

            var loginResponse = await OpenSubtitles.LogInAsync(user, password, "en", cancellationToken).ConfigureAwait(false);

            if (!(loginResponse is MethodResponseLogIn))
            {
                throw new UnauthorizedAccessException("Authentication to OpenSubtitles failed.");
            }

            _lastLogin = DateTime.UtcNow;
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            var imdbIdText = request.GetProviderId(MetadataProviders.Imdb);
            long imdbId = 0;

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
                    if (string.IsNullOrWhiteSpace(imdbIdText) || !long.TryParse(imdbIdText.TrimStart('t'), NumberStyles.Any, _usCulture, out imdbId))
                    {
                        _logger.Debug("Imdb id missing");
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

            await Login(cancellationToken).ConfigureAwait(false);

            var subLanguageId = request.Language;
            var hash = Utilities.ComputeHash(request.MediaPath);
            var fileInfo = new FileInfo(request.MediaPath);
            var movieByteSize = fileInfo.Length;
            var searchImdbId = request.ContentType == SubtitleMediaType.Movie ? imdbId.ToString(_usCulture) : "";
            var subtitleSearchParameters = request.ContentType == SubtitleMediaType.Episode
                ? new List<SubtitleSearchParameters> {
                                                         new SubtitleSearchParameters(subLanguageId, 
                                                             query: request.SeriesName,
                                                             season: request.ParentIndexNumber.Value.ToString(_usCulture),
                                                             episode: request.IndexNumber.Value.ToString(_usCulture))
                                                     }
                : new List<SubtitleSearchParameters> {
                                                         new SubtitleSearchParameters(subLanguageId, imdbid: searchImdbId),
                                                         new SubtitleSearchParameters(subLanguageId, query: request.Name, imdbid: searchImdbId)
                                                     };
            var parms = new List<SubtitleSearchParameters> {
                                                               new SubtitleSearchParameters( subLanguageId, 
                                                                   movieHash: hash, 
                                                                   movieByteSize: movieByteSize, 
                                                                   imdbid: searchImdbId ),
                                                           };
            parms.AddRange(subtitleSearchParameters);
            var result = await OpenSubtitles.SearchSubtitlesAsync(parms.ToArray(), cancellationToken).ConfigureAwait(false);
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

                        Id = i.SubFormat + "-" + i.SubLanguageID + "-" + i.IDSubtitleFile,

                        Name = i.SubFileName,
                        DateCreated = DateTime.Parse(i.SubAddDate, _usCulture),
                        IsHashMatch = i.MovieHash == hasCopy
                    });
        }

        public void Dispose()
        {
            _config.ConfigurationUpdating -= _config_ConfigurationUpdating;
        }
    }
}
