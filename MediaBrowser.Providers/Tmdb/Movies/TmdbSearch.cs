using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.Search;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.Movies
{
    public class TmdbSearch
    {
        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private static readonly Regex _cleanEnclosed = new Regex(@"\p{Ps}.*\p{Pe}", RegexOptions.Compiled);
        private static readonly Regex _cleanNonWord = new Regex(@"[\W_]+", RegexOptions.Compiled);
        private static readonly Regex _cleanStopWords = new Regex(@"\b( # Start at word boundary
            19[0-9]{2}|20[0-9]{2}| # 1900-2099
            S[0-9]{2}| # Season
            E[0-9]{2}| # Episode
            (2160|1080|720|576|480)[ip]?| # Resolution
            [xh]?264| # Encoding
            (web|dvd|bd|hdtv|hd)rip| # *Rip
            web|hdtv|mp4|bluray|ktr|dl|single|imageset|internal|doku|dubbed|retail|xxx|flac
            ).* # Match rest of string",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

        private const string _searchURL = TmdbUtils.BaseTmdbApiUrl + @"3/search/{3}?api_key={1}&query={0}&language={2}";

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly ILibraryManager _libraryManager;

        public TmdbSearch(ILogger logger, IJsonSerializer json, ILibraryManager libraryManager)
        {
            _logger = logger;
            _json = json;
            _libraryManager = libraryManager;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo idInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(idInfo, "tv", cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetMovieSearchResults(ItemLookupInfo idInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(idInfo, "movie", cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo idInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(idInfo, "collection", cancellationToken);
        }

        private async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ItemLookupInfo idInfo, string searchType, CancellationToken cancellationToken)
        {
            var name = idInfo.Name;
            var year = idInfo.Year;

            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<RemoteSearchResult>();
            }

            var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

            // TODO: Investigate: Does this mean we are reparsing already parsed ItemLookupInfo?
            var parsedName = _libraryManager.ParseName(name);
            var yearInName = parsedName.Year;
            name = parsedName.Name;
            year ??= yearInName;

            _logger.LogInformation("TmdbSearch: Finding id for item: {0} ({1})", name, year);
            var language = idInfo.MetadataLanguage.ToLowerInvariant();

            // Replace sequences of non-word characters with space
            // TMDB expects a space separated list of words make sure that is the case
            name = _cleanNonWord.Replace(name, " ").Trim();

            var results = await GetSearchResults(name, searchType, year, language, tmdbImageUrl, cancellationToken).ConfigureAwait(false);

            if (results.Count == 0)
            {
                //try in english if wasn't before
                if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    results = await GetSearchResults(name, searchType, year, "en", tmdbImageUrl, cancellationToken).ConfigureAwait(false);
                }
            }

            // TODO: retrying alternatives should be done outside the search
            // provider so that the retry logic can be common for all search
            // providers
            if (results.Count == 0)
            {
                var name2 = parsedName.Name;

                // Remove things enclosed in []{}() etc
                name2 = _cleanEnclosed.Replace(name2, string.Empty);

                // Replace sequences of non-word characters with space
                name2 = _cleanNonWord.Replace(name2, " ");

                // Clean based on common stop words / tokens
                name2 = _cleanStopWords.Replace(name2, string.Empty);

                // Trim whitespace
                name2 = name2.Trim();

                // Search again if the new name is different
                if (!string.Equals(name2, name) && !string.IsNullOrWhiteSpace(name2))
                {
                    _logger.LogInformation("TmdbSearch: Finding id for item: {0} ({1})", name2, year);
                    results = await GetSearchResults(name2, searchType, year, language, tmdbImageUrl, cancellationToken).ConfigureAwait(false);

                    if (results.Count == 0 && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        //one more time, in english
                        results = await GetSearchResults(name2, searchType, year, "en", tmdbImageUrl, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return results.Where(i =>
            {
                if (year.HasValue && i.ProductionYear.HasValue)
                {
                    // Allow one year tolerance
                    return Math.Abs(year.Value - i.ProductionYear.Value) <= 1;
                }

                return true;
            });
        }

        private Task<List<RemoteSearchResult>> GetSearchResults(string name, string type, int? year, string language, string baseImageUrl, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case "tv":
                    return GetSearchResultsTv(name, year, language, baseImageUrl, cancellationToken);
                default:
                    return GetSearchResultsGeneric(name, type, year, language, baseImageUrl, cancellationToken);
            }
        }

        private async Task<List<RemoteSearchResult>> GetSearchResultsGeneric(string name, string type, int? year, string language, string baseImageUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            var url3 = string.Format(_searchURL, WebUtility.UrlEncode(name), TmdbUtils.ApiKey, language, type);

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url3,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    var searchResults = await _json.DeserializeFromStreamAsync<TmdbSearchResult<MovieResult>>(json).ConfigureAwait(false);

                    var results = searchResults.Results ?? new List<MovieResult>();

                    return results
                        .Select(i =>
                        {
                            var remoteResult = new RemoteSearchResult
                            {
                                SearchProviderName = TmdbMovieProvider.Current.Name,
                                Name = i.Title ?? i.Name ?? i.Original_Title,
                                ImageUrl = string.IsNullOrWhiteSpace(i.Poster_Path) ? null : baseImageUrl + i.Poster_Path
                            };

                            if (!string.IsNullOrWhiteSpace(i.Release_Date))
                            {
                                // These dates are always in this exact format
                                if (DateTime.TryParseExact(i.Release_Date, "yyyy-MM-dd", _usCulture, DateTimeStyles.None, out var r))
                                {
                                    remoteResult.PremiereDate = r.ToUniversalTime();
                                    remoteResult.ProductionYear = remoteResult.PremiereDate.Value.Year;
                                }
                            }

                            remoteResult.SetProviderId(MetadataProviders.Tmdb, i.Id.ToString(_usCulture));

                            return remoteResult;

                        })
                        .ToList();
                }
            }
        }

        private async Task<List<RemoteSearchResult>> GetSearchResultsTv(string name, int? year, string language, string baseImageUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            var url3 = string.Format(_searchURL, WebUtility.UrlEncode(name), TmdbUtils.ApiKey, language, "tv");

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url3,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    var searchResults = await _json.DeserializeFromStreamAsync<TmdbSearchResult<TvResult>>(json).ConfigureAwait(false);

                    var results = searchResults.Results ?? new List<TvResult>();

                    return results
                        .Select(i =>
                        {
                            var remoteResult = new RemoteSearchResult
                            {
                                SearchProviderName = TmdbMovieProvider.Current.Name,
                                Name = i.Name ?? i.Original_Name,
                                ImageUrl = string.IsNullOrWhiteSpace(i.Poster_Path) ? null : baseImageUrl + i.Poster_Path
                            };

                            if (!string.IsNullOrWhiteSpace(i.First_Air_Date))
                            {
                                // These dates are always in this exact format
                                if (DateTime.TryParseExact(i.First_Air_Date, "yyyy-MM-dd", _usCulture, DateTimeStyles.None, out var r))
                                {
                                    remoteResult.PremiereDate = r.ToUniversalTime();
                                    remoteResult.ProductionYear = remoteResult.PremiereDate.Value.Year;
                                }
                            }

                            remoteResult.SetProviderId(MetadataProviders.Tmdb, i.Id.ToString(_usCulture));

                            return remoteResult;

                        })
                        .ToList();
                }
            }
        }
    }
}
