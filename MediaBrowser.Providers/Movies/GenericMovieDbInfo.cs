using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Providers.Movies
{
    public class GenericMovieDbInfo<T>
        where T : BaseItem, new()
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILibraryManager _libraryManager;
		private readonly IFileSystem _fileSystem;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

		public GenericMovieDbInfo(ILogger logger, IJsonSerializer jsonSerializer, ILibraryManager libraryManager, IFileSystem fileSystem)
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
                var searchResults = await new MovieDbSearch(_logger, _jsonSerializer, _libraryManager).GetMovieSearchResults(itemId, cancellationToken).ConfigureAwait(false);

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
            MovieDbProvider.CompleteMovieData movieInfo = null;

            // Id could be ImdbId or TmdbId
            if (string.IsNullOrEmpty(tmdbId))
            {
                movieInfo = await MovieDbProvider.Current.FetchMainResult(imdbId, false, language, cancellationToken).ConfigureAwait(false);
                if (movieInfo != null)
                {
                    tmdbId = movieInfo.id.ToString(_usCulture);

                    dataFilePath = MovieDbProvider.Current.GetDataFilePath(tmdbId, language);
                    _fileSystem.CreateDirectory(Path.GetDirectoryName(dataFilePath));
                    _jsonSerializer.SerializeToFile(movieInfo, dataFilePath);
                }
            }

            if (!string.IsNullOrWhiteSpace(tmdbId))
            {
                await MovieDbProvider.Current.EnsureMovieInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

                dataFilePath = dataFilePath ?? MovieDbProvider.Current.GetDataFilePath(tmdbId, language);
                movieInfo = movieInfo ?? _jsonSerializer.DeserializeFromFile<MovieDbProvider.CompleteMovieData>(dataFilePath);

                var settings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

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
        private void ProcessMainInfo(MetadataResult<T> resultItem, TmdbSettingsResult settings, string preferredCountryCode, MovieDbProvider.CompleteMovieData movieData)
        {
            var movie = resultItem.Item;

            movie.Name = movieData.GetTitle() ?? movie.Name;

            movie.OriginalTitle = movieData.GetOriginalTitle();

            // Bug in Mono: WebUtility.HtmlDecode should return null if the string is null but in Mono it generate an System.ArgumentNullException.
            movie.Overview = movieData.overview != null ? WebUtility.HtmlDecode(movieData.overview) : null;
            movie.Overview = movie.Overview != null ? movie.Overview.Replace("\n\n", "\n") : null;

            movie.HomePageUrl = movieData.homepage;

            var hasBudget = movie as IHasBudget;
            if (hasBudget != null)
            {
                hasBudget.Budget = movieData.budget;
                hasBudget.Revenue = movieData.revenue;
            }

            if (!string.IsNullOrEmpty(movieData.tagline))
            {
                movie.Tagline = movieData.tagline;
            }

            if (movieData.production_countries != null)
            {
                movie.ProductionLocations = movieData
                    .production_countries
                    .Select(i => i.name)
                    .ToList();
            }

            movie.SetProviderId(MetadataProviders.Tmdb, movieData.id.ToString(_usCulture));
            movie.SetProviderId(MetadataProviders.Imdb, movieData.imdb_id);

            if (movieData.belongs_to_collection != null)
            {
                movie.SetProviderId(MetadataProviders.TmdbCollection,
                                    movieData.belongs_to_collection.id.ToString(CultureInfo.InvariantCulture));

                var movieItem = movie as Movie;

                if (movieItem != null)
                {
                    movieItem.CollectionName = movieData.belongs_to_collection.name;
                }
            }

            float rating;
            string voteAvg = movieData.vote_average.ToString(CultureInfo.InvariantCulture);

            if (float.TryParse(voteAvg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out rating))
            {
                movie.CommunityRating = rating;
            }

            movie.VoteCount = movieData.vote_count;

            //release date and certification are retrieved based on configured country and we fall back on US if not there and to minimun release date if still no match
            if (movieData.releases != null && movieData.releases.countries != null)
            {
                var releases = movieData.releases.countries.Where(i => !string.IsNullOrWhiteSpace(i.certification)).ToList();

                var ourRelease = releases.FirstOrDefault(c => string.Equals(c.iso_3166_1, preferredCountryCode, StringComparison.OrdinalIgnoreCase));
                var usRelease = releases.FirstOrDefault(c => string.Equals(c.iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));

                if (ourRelease != null)
                {
                    var ratingPrefix = string.Equals(preferredCountryCode, "us", StringComparison.OrdinalIgnoreCase) ? "" : preferredCountryCode + "-";
                    var newRating = ratingPrefix + ourRelease.certification;

                    newRating = newRating.Replace("de-", "FSK-", StringComparison.OrdinalIgnoreCase);

                    movie.OfficialRating = newRating;
                }
                else if (usRelease != null)
                {
                    movie.OfficialRating = usRelease.certification;
                }
            }

            if (!string.IsNullOrWhiteSpace(movieData.release_date))
            {
                DateTime r;

                // These dates are always in this exact format
                if (DateTime.TryParse(movieData.release_date, _usCulture, DateTimeStyles.None, out r))
                {
                    movie.PremiereDate = r.ToUniversalTime();
                    movie.ProductionYear = movie.PremiereDate.Value.Year;
                }
            }

            //studios
            if (movieData.production_companies != null)
            {
                movie.Studios.Clear();

                foreach (var studio in movieData.production_companies.Select(c => c.name))
                {
                    movie.AddStudio(studio);
                }
            }

            // genres
            // Movies get this from imdb
            var genres = movieData.genres ?? new List<MovieDbProvider.GenreItem>();

            foreach (var genre in genres.Select(g => g.name))
            {
                movie.AddGenre(genre);
            }

            resultItem.ResetPeople();
            var tmdbImageUrl = settings.images.secure_base_url + "original";

            //Actors, Directors, Writers - all in People
            //actors come from cast
            if (movieData.casts != null && movieData.casts.cast != null)
            {
                foreach (var actor in movieData.casts.cast.OrderBy(a => a.order))
                {
                    var personInfo = new PersonInfo
                    {
                        Name = actor.name.Trim(),
                        Role = actor.character,
                        Type = PersonType.Actor,
                        SortOrder = actor.order
                    };

                    if (!string.IsNullOrWhiteSpace(actor.profile_path))
                    {
                        personInfo.ImageUrl = tmdbImageUrl + actor.profile_path;
                    }

                    if (actor.id > 0)
                    {
                        personInfo.SetProviderId(MetadataProviders.Tmdb, actor.id.ToString(CultureInfo.InvariantCulture));
                    }

                    resultItem.AddPerson(personInfo);
                }
            }

            //and the rest from crew
            if (movieData.casts != null && movieData.casts.crew != null)
            {
                foreach (var person in movieData.casts.crew)
                {
                    // Normalize this
                    var type = person.department;
                    if (string.Equals(type, "writing", StringComparison.OrdinalIgnoreCase))
                    {
                        type = PersonType.Writer;
                    }

                    var personInfo = new PersonInfo
                    {
                        Name = person.name.Trim(),
                        Role = person.job,
                        Type = type
                    };

                    if (!string.IsNullOrWhiteSpace(person.profile_path))
                    {
                        personInfo.ImageUrl = tmdbImageUrl + person.profile_path;
                    }

                    if (person.id > 0)
                    {
                        personInfo.SetProviderId(MetadataProviders.Tmdb, person.id.ToString(CultureInfo.InvariantCulture));
                    }

                    resultItem.AddPerson(personInfo);
                }
            }

            if (movieData.keywords != null && movieData.keywords.keywords != null)
            {
                movie.Keywords = movieData.keywords.keywords.Select(i => i.name).ToList();
            }

            if (movieData.trailers != null && movieData.trailers.youtube != null &&
                movieData.trailers.youtube.Count > 0)
            {
                var hasTrailers = movie as IHasTrailers;
                if (hasTrailers != null)
                {
                    hasTrailers.RemoteTrailers = movieData.trailers.youtube.Select(i => new MediaUrl
                    {
                        Url = string.Format("https://www.youtube.com/watch?v={0}", i.source),
                        Name = i.name

                    }).ToList();
                }
            }
        }

    }
}
