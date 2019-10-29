using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly CultureInfo EnUs = new CultureInfo("en-US");
        private const string Search3 = TmdbUtils.BaseTmdbApiUrl + @"3/search/{3}?api_key={1}&query={0}&language={2}";

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

            if (!string.IsNullOrWhiteSpace(name))
            {
                var parsedName = _libraryManager.ParseName(name);
                var yearInName = parsedName.Year;
                name = parsedName.Name;
                year = year ?? yearInName;
            }

            _logger.LogInformation("MovieDbProvider: Finding id for item: " + name);
            var language = idInfo.MetadataLanguage.ToLowerInvariant();

            //nope - search for it
            //var searchType = item is BoxSet ? "collection" : "movie";

            var results = await GetSearchResults(name, searchType, year, language, tmdbImageUrl, cancellationToken).ConfigureAwait(false);

            if (results.Count == 0)
            {
                //try in english if wasn't before
                if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    results = await GetSearchResults(name, searchType, year, "en", tmdbImageUrl, cancellationToken).ConfigureAwait(false);
                }
            }

            if (results.Count == 0)
            {
                // try with dot and _ turned to space
                var originalName = name;

                name = name.Replace(",", " ");
                name = name.Replace(".", " ");
                name = name.Replace("_", " ");
                name = name.Replace("-", " ");
                name = name.Replace("!", " ");
                name = name.Replace("?", " ");

                var parenthIndex = name.IndexOf('(');
                if (parenthIndex != -1)
                {
                    name = name.Substring(0, parenthIndex);
                }

                name = name.Trim();

                // Search again if the new name is different
                if (!string.Equals(name, originalName))
                {
                    results = await GetSearchResults(name, searchType, year, language, tmdbImageUrl, cancellationToken).ConfigureAwait(false);

                    if (results.Count == 0 && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        //one more time, in english
                        results = await GetSearchResults(name, searchType, year, "en", tmdbImageUrl, cancellationToken).ConfigureAwait(false);

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

            var url3 = string.Format(Search3, WebUtility.UrlEncode(name), TmdbUtils.ApiKey, language, type);

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
                                if (DateTime.TryParseExact(i.Release_Date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out var r))
                                {
                                    remoteResult.PremiereDate = r.ToUniversalTime();
                                    remoteResult.ProductionYear = remoteResult.PremiereDate.Value.Year;
                                }
                            }

                            remoteResult.SetProviderId(MetadataProviders.Tmdb, i.Id.ToString(EnUs));

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

            var url3 = string.Format(Search3, WebUtility.UrlEncode(name), TmdbUtils.ApiKey, language, "tv");

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
                                if (DateTime.TryParseExact(i.First_Air_Date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out var r))
                                {
                                    remoteResult.PremiereDate = r.ToUniversalTime();
                                    remoteResult.ProductionYear = remoteResult.PremiereDate.Value.Year;
                                }
                            }

                            remoteResult.SetProviderId(MetadataProviders.Tmdb, i.Id.ToString(EnUs));

                            return remoteResult;

                        })
                        .ToList();
                }
            }
        }
    }
}
