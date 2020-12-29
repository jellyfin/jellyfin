#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.Movies
{
    /// <summary>
    /// Class MovieDbProvider.
    /// </summary>
    public class TmdbMovieProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILibraryManager _libraryManager;
        private readonly TmdbClientManager _tmdbClientManager;

        public TmdbMovieProvider(
            ILibraryManager libraryManager,
            TmdbClientManager tmdbClientManager,
            IHttpClientFactory httpClientFactory)
        {
            _libraryManager = libraryManager;
            _tmdbClientManager = tmdbClientManager;
            _httpClientFactory = httpClientFactory;
        }

        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public int Order => 1;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(searchInfo.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (tmdbId == 0)
            {
                var movieResults = await _tmdbClientManager
                    .SearchMovieAsync(searchInfo.Name, searchInfo.MetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);
                var remoteSearchResults = new List<RemoteSearchResult>();
                for (var i = 0; i < movieResults.Count; i++)
                {
                    var movieResult = movieResults[i];
                    var remoteSearchResult = new RemoteSearchResult
                    {
                        Name = movieResult.Title ?? movieResult.OriginalTitle,
                        ImageUrl = _tmdbClientManager.GetPosterUrl(movieResult.PosterPath),
                        Overview = movieResult.Overview,
                        SearchProviderName = Name
                    };

                    var releaseDate = movieResult.ReleaseDate?.ToUniversalTime();
                    remoteSearchResult.PremiereDate = releaseDate;
                    remoteSearchResult.ProductionYear = releaseDate?.Year;

                    remoteSearchResult.SetProviderId(MetadataProvider.Tmdb, movieResult.Id.ToString(CultureInfo.InvariantCulture));
                    remoteSearchResults.Add(remoteSearchResult);
                }

                return remoteSearchResults;
            }

            var movie = await _tmdbClientManager
                .GetMovieAsync(tmdbId, searchInfo.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(searchInfo.MetadataLanguage), cancellationToken)
                .ConfigureAwait(false);

            var remoteResult = new RemoteSearchResult
            {
                Name = movie.Title ?? movie.OriginalTitle,
                SearchProviderName = Name,
                ImageUrl = _tmdbClientManager.GetPosterUrl(movie.PosterPath),
                Overview = movie.Overview
            };

            if (movie.ReleaseDate != null)
            {
                var releaseDate = movie.ReleaseDate.Value.ToUniversalTime();
                remoteResult.PremiereDate = releaseDate;
                remoteResult.ProductionYear = releaseDate.Year;
            }

            remoteResult.SetProviderId(MetadataProvider.Tmdb, movie.Id.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(movie.ImdbId))
            {
                remoteResult.SetProviderId(MetadataProvider.Imdb, movie.ImdbId);
            }

            return new[] { remoteResult };
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var tmdbId = info.GetProviderId(MetadataProvider.Tmdb);
            var imdbId = info.GetProviderId(MetadataProvider.Imdb);

            if (string.IsNullOrEmpty(tmdbId) && string.IsNullOrEmpty(imdbId))
            {
                // ParseName is required here.
                // Caller provides the filename with extension stripped and NOT the parsed filename
                var parsedName = _libraryManager.ParseName(info.Name);
                var searchResults = await _tmdbClientManager.SearchMovieAsync(parsedName.Name, parsedName.Year ?? 0, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                if (searchResults.Count > 0)
                {
                    tmdbId = searchResults[0].Id.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrEmpty(tmdbId))
            {
                return new MetadataResult<Movie>();
            }

            var movieResult = await _tmdbClientManager
                .GetMovieAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), info.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(info.MetadataLanguage), cancellationToken)
                .ConfigureAwait(false);

            if (movieResult == null)
            {
                return new MetadataResult<Movie>();
            }

            var movie = new Movie
            {
                Name = movieResult.Title ?? movieResult.OriginalTitle,
                Overview = movieResult.Overview?.Replace("\n\n", "\n", StringComparison.InvariantCulture),
                Tagline = movieResult.Tagline,
                ProductionLocations = movieResult.ProductionCountries.Select(pc => pc.Name).ToArray()
            };
            var metadataResult = new MetadataResult<Movie>
            {
                HasMetadata = true,
                ResultLanguage = info.MetadataLanguage,
                Item = movie
            };

            movie.SetProviderId(MetadataProvider.Tmdb, tmdbId);
            movie.SetProviderId(MetadataProvider.Imdb, movieResult.ImdbId);
            if (movieResult.BelongsToCollection != null)
            {
                movie.SetProviderId(MetadataProvider.TmdbCollection, movieResult.BelongsToCollection.Id.ToString(CultureInfo.InvariantCulture));
                movie.CollectionName = movieResult.BelongsToCollection.Name;
            }

            movie.CommunityRating = Convert.ToSingle(movieResult.VoteAverage);

            if (movieResult.Releases?.Countries != null)
            {
                var releases = movieResult.Releases.Countries.Where(i => !string.IsNullOrWhiteSpace(i.Certification)).ToList();

                var ourRelease = releases.FirstOrDefault(c => string.Equals(c.Iso_3166_1, info.MetadataCountryCode, StringComparison.OrdinalIgnoreCase));
                var usRelease = releases.FirstOrDefault(c => string.Equals(c.Iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));

                if (ourRelease != null)
                {
                    var ratingPrefix = string.Equals(info.MetadataCountryCode, "us", StringComparison.OrdinalIgnoreCase) ? string.Empty : info.MetadataCountryCode + "-";
                    var newRating = ratingPrefix + ourRelease.Certification;

                    newRating = newRating.Replace("de-", "FSK-", StringComparison.OrdinalIgnoreCase);

                    movie.OfficialRating = newRating;
                }
                else if (usRelease != null)
                {
                    movie.OfficialRating = usRelease.Certification;
                }
            }

            movie.PremiereDate = movieResult.ReleaseDate;
            movie.ProductionYear = movieResult.ReleaseDate?.Year;

            if (movieResult.ProductionCompanies != null)
            {
                movie.SetStudios(movieResult.ProductionCompanies.Select(c => c.Name));
            }

            var genres = movieResult.Genres;

            foreach (var genre in genres.Select(g => g.Name))
            {
                movie.AddGenre(genre);
            }

            if (movieResult.Keywords?.Keywords != null)
            {
                for (var i = 0; i < movieResult.Keywords.Keywords.Count; i++)
                {
                    movie.AddTag(movieResult.Keywords.Keywords[i].Name);
                }
            }

            if (movieResult.Credits?.Cast != null)
            {
                // TODO configurable
                foreach (var actor in movieResult.Credits.Cast.OrderBy(a => a.Order).Take(TmdbUtils.MaxCastMembers))
                {
                    var personInfo = new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character,
                        Type = PersonType.Actor,
                        SortOrder = actor.Order
                    };

                    if (!string.IsNullOrWhiteSpace(actor.ProfilePath))
                    {
                        personInfo.ImageUrl = _tmdbClientManager.GetProfileUrl(actor.ProfilePath);
                    }

                    if (actor.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProvider.Tmdb, actor.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    metadataResult.AddPerson(personInfo);
                }
            }

            if (movieResult.Credits?.Crew != null)
            {
                var keepTypes = new[]
                {
                    PersonType.Director,
                    PersonType.Writer,
                    PersonType.Producer
                };

                foreach (var person in movieResult.Credits.Crew)
                {
                    // Normalize this
                    var type = TmdbUtils.MapCrewToPersonType(person);

                    if (!keepTypes.Contains(type, StringComparer.OrdinalIgnoreCase) &&
                        !keepTypes.Contains(person.Job ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var personInfo = new PersonInfo
                    {
                        Name = person.Name.Trim(),
                        Role = person.Job,
                        Type = type
                    };

                    if (!string.IsNullOrWhiteSpace(person.ProfilePath))
                    {
                        personInfo.ImageUrl = _tmdbClientManager.GetPosterUrl(person.ProfilePath);
                    }

                    if (person.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProvider.Tmdb, person.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    metadataResult.AddPerson(personInfo);
                }
            }

            if (movieResult.Videos?.Results != null)
            {
                var trailers = new List<MediaUrl>();
                for (var i = 0; i < movieResult.Videos.Results.Count; i++)
                {
                    var video = movieResult.Videos.Results[0];
                    if (!TmdbUtils.IsTrailerType(video))
                    {
                        continue;
                    }

                    trailers.Add(new MediaUrl
                    {
                        Url = string.Format(CultureInfo.InvariantCulture, "https://www.youtube.com/watch?v={0}", video.Key),
                        Name = video.Name
                    });
                }

                movie.RemoteTrailers = trailers;
            }

            return metadataResult;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
