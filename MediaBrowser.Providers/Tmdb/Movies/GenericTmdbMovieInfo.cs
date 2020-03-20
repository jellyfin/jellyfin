using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.Movies;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.Movies
{
    public class GenericTmdbMovieInfo<T>
        where T : BaseItem, new()
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public GenericTmdbMovieInfo(ILogger logger, IJsonSerializer jsonSerializer, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
        }

        public async Task<MetadataResult<T>> GetMetadata(ItemLookupInfo itemId, CancellationToken cancellationToken)
        {
            var tmdbId = itemId.GetProviderId(MetadataProviders.Tmdb);
            var imdbId = itemId.GetProviderId(MetadataProviders.Imdb);

            // Don't search for music video id's because it is very easy to misidentify.
            if (string.IsNullOrEmpty(tmdbId) && string.IsNullOrEmpty(imdbId) && typeof(T) != typeof(MusicVideo))
            {
                var searchResults = await new TmdbSearch(_logger, _jsonSerializer, _libraryManager).GetMovieSearchResults(itemId, cancellationToken).ConfigureAwait(false);

                var searchResult = searchResults.FirstOrDefault();

                if (searchResult != null)
                {
                    tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                }
            }

            if (!string.IsNullOrEmpty(tmdbId) || !string.IsNullOrEmpty(imdbId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await FetchMovieData(tmdbId, imdbId, itemId.MetadataLanguage, itemId.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
            }

            return new MetadataResult<T>();
        }

        /// <summary>
        /// Fetches the movie data.
        /// </summary>
        /// <param name="tmdbId">The TMDB identifier.</param>
        /// <param name="imdbId">The imdb identifier.</param>
        /// <param name="language">The language.</param>
        /// <param name="preferredCountryCode">The preferred country code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{`0}.</returns>
        private async Task<MetadataResult<T>> FetchMovieData(string tmdbId, string imdbId, string language, string preferredCountryCode, CancellationToken cancellationToken)
        {
            var item = new MetadataResult<T>
            {
                Item = new T()
            };

            string dataFilePath = null;
            MovieResult movieInfo = null;

            // Id could be ImdbId or TmdbId
            if (string.IsNullOrEmpty(tmdbId))
            {
                movieInfo = await TmdbMovieProvider.Current.FetchMainResult(imdbId, false, language, cancellationToken).ConfigureAwait(false);
                if (movieInfo != null)
                {
                    tmdbId = movieInfo.Id.ToString(_usCulture);

                    dataFilePath = TmdbMovieProvider.Current.GetDataFilePath(tmdbId, language);
                    Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));
                    _jsonSerializer.SerializeToFile(movieInfo, dataFilePath);
                }
            }

            if (!string.IsNullOrWhiteSpace(tmdbId))
            {
                await TmdbMovieProvider.Current.EnsureMovieInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

                dataFilePath = dataFilePath ?? TmdbMovieProvider.Current.GetDataFilePath(tmdbId, language);
                movieInfo = movieInfo ?? _jsonSerializer.DeserializeFromFile<MovieResult>(dataFilePath);

                var settings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                ProcessMainInfo(item, settings, preferredCountryCode, movieInfo);
                item.HasMetadata = true;
            }

            return item;
        }

        /// <summary>
        /// Processes the main info.
        /// </summary>
        /// <param name="resultItem">The result item.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="preferredCountryCode">The preferred country code.</param>
        /// <param name="movieData">The movie data.</param>
        private void ProcessMainInfo(MetadataResult<T> resultItem, TmdbSettingsResult settings, string preferredCountryCode, MovieResult movieData)
        {
            var movie = resultItem.Item;

            movie.Name = movieData.GetTitle() ?? movie.Name;

            movie.OriginalTitle = movieData.GetOriginalTitle();

            movie.Overview = string.IsNullOrWhiteSpace(movieData.Overview) ? null : WebUtility.HtmlDecode(movieData.Overview);
            movie.Overview = movie.Overview != null ? movie.Overview.Replace("\n\n", "\n") : null;

            //movie.HomePageUrl = movieData.homepage;

            if (!string.IsNullOrEmpty(movieData.Tagline))
            {
                movie.Tagline = movieData.Tagline;
            }

            if (movieData.Production_Countries != null)
            {
                movie.ProductionLocations = movieData
                    .Production_Countries
                    .Select(i => i.Name)
                    .ToArray();
            }

            movie.SetProviderId(MetadataProviders.Tmdb, movieData.Id.ToString(_usCulture));
            movie.SetProviderId(MetadataProviders.Imdb, movieData.Imdb_Id);

            if (movieData.Belongs_To_Collection != null)
            {
                movie.SetProviderId(MetadataProviders.TmdbCollection,
                                    movieData.Belongs_To_Collection.Id.ToString(CultureInfo.InvariantCulture));

                if (movie is Movie movieItem)
                {
                    movieItem.CollectionName = movieData.Belongs_To_Collection.Name;
                }
            }

            string voteAvg = movieData.Vote_Average.ToString(CultureInfo.InvariantCulture);

            if (float.TryParse(voteAvg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rating))
            {
                movie.CommunityRating = rating;
            }

            //movie.VoteCount = movieData.vote_count;

            if (movieData.Releases != null && movieData.Releases.Countries != null)
            {
                var releases = movieData.Releases.Countries.Where(i => !string.IsNullOrWhiteSpace(i.Certification)).ToList();

                var ourRelease = releases.FirstOrDefault(c => string.Equals(c.Iso_3166_1, preferredCountryCode, StringComparison.OrdinalIgnoreCase));
                var usRelease = releases.FirstOrDefault(c => string.Equals(c.Iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));

                if (ourRelease != null)
                {
                    var ratingPrefix = string.Equals(preferredCountryCode, "us", StringComparison.OrdinalIgnoreCase) ? "" : preferredCountryCode + "-";
                    var newRating = ratingPrefix + ourRelease.Certification;

                    newRating = newRating.Replace("de-", "FSK-", StringComparison.OrdinalIgnoreCase);

                    movie.OfficialRating = newRating;
                }
                else if (usRelease != null)
                {
                    movie.OfficialRating = usRelease.Certification;
                }
            }

            if (!string.IsNullOrWhiteSpace(movieData.Release_Date))
            {
                // These dates are always in this exact format
                if (DateTime.TryParse(movieData.Release_Date, _usCulture, DateTimeStyles.None, out var r))
                {
                    movie.PremiereDate = r.ToUniversalTime();
                    movie.ProductionYear = movie.PremiereDate.Value.Year;
                }
            }

            //studios
            if (movieData.Production_Companies != null)
            {
                movie.SetStudios(movieData.Production_Companies.Select(c => c.Name));
            }

            // genres
            // Movies get this from imdb
            var genres = movieData.Genres ?? new List<Tmdb.Models.General.Genre>();

            foreach (var genre in genres.Select(g => g.Name))
            {
                movie.AddGenre(genre);
            }

            resultItem.ResetPeople();
            var tmdbImageUrl = settings.images.GetImageUrl("original");

            //Actors, Directors, Writers - all in People
            //actors come from cast
            if (movieData.Casts != null && movieData.Casts.Cast != null)
            {
                foreach (var actor in movieData.Casts.Cast.OrderBy(a => a.Order))
                {
                    var personInfo = new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character,
                        Type = PersonType.Actor,
                        SortOrder = actor.Order
                    };

                    if (!string.IsNullOrWhiteSpace(actor.Profile_Path))
                    {
                        personInfo.ImageUrl = tmdbImageUrl + actor.Profile_Path;
                    }

                    if (actor.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProviders.Tmdb, actor.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    resultItem.AddPerson(personInfo);
                }
            }

            //and the rest from crew
            if (movieData.Casts?.Crew != null)
            {
                var keepTypes = new[]
                {
                    PersonType.Director,
                    PersonType.Writer,
                    PersonType.Producer
                };

                foreach (var person in movieData.Casts.Crew)
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

                    if (!string.IsNullOrWhiteSpace(person.Profile_Path))
                    {
                        personInfo.ImageUrl = tmdbImageUrl + person.Profile_Path;
                    }

                    if (person.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProviders.Tmdb, person.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    resultItem.AddPerson(personInfo);
                }
            }

            //if (movieData.keywords != null && movieData.keywords.keywords != null)
            //{
            //    movie.Keywords = movieData.keywords.keywords.Select(i => i.name).ToList();
            //}

            if (movieData.Trailers != null && movieData.Trailers.Youtube != null)
            {
                movie.RemoteTrailers = movieData.Trailers.Youtube.Select(i => new MediaUrl
                {
                    Url = string.Format("https://www.youtube.com/watch?v={0}", i.Source),
                    Name = i.Name

                }).ToArray();
            }
        }

    }
}
