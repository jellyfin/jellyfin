using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
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
using PersonInfo = MediaBrowser.Controller.Entities.PersonInfo;

namespace MediaBrowser.Providers.Movies
{
    public class GenericMovieDbInfo<T>
        where T : Video, new()
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public GenericMovieDbInfo(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<MetadataResult<T>> GetMetadata(ItemLookupInfo itemId, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

            var tmdbId = itemId.GetProviderId(MetadataProviders.Tmdb);
            var imdbId = itemId.GetProviderId(MetadataProviders.Imdb);

            // Don't search for music video id's because it is very easy to misidentify. 
            if (string.IsNullOrEmpty(tmdbId) && string.IsNullOrEmpty(imdbId) && typeof(T) != typeof(MusicVideo))
            {
                tmdbId = await new MovieDbSearch(_logger, _jsonSerializer)
                    .FindMovieId(itemId, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(tmdbId) || !string.IsNullOrEmpty(imdbId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.Item = await FetchMovieData(tmdbId, imdbId, itemId.MetadataLanguage, itemId.MetadataCountryCode, cancellationToken).ConfigureAwait(false);

                result.HasMetadata = result.Item != null;
            }

            return result;
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
        private async Task<T> FetchMovieData(string tmdbId, string imdbId, string language, string preferredCountryCode, CancellationToken cancellationToken)
        {
            string dataFilePath = null;
            MovieDbProvider.CompleteMovieData movieInfo = null;

            // Id could be ImdbId or TmdbId
            if (string.IsNullOrEmpty(tmdbId))
            {
                movieInfo = await MovieDbProvider.Current.FetchMainResult(imdbId, language, cancellationToken).ConfigureAwait(false);
                if (movieInfo == null) return null;

                tmdbId = movieInfo.id.ToString(_usCulture);

                dataFilePath = MovieDbProvider.Current.GetDataFilePath(tmdbId, language);
                Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));
                _jsonSerializer.SerializeToFile(movieInfo, dataFilePath);
            }

            await MovieDbProvider.Current.EnsureMovieInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            dataFilePath = dataFilePath ?? MovieDbProvider.Current.GetDataFilePath(tmdbId, language);
            movieInfo = movieInfo ?? _jsonSerializer.DeserializeFromFile<MovieDbProvider.CompleteMovieData>(dataFilePath);

            var item = new T();

            ProcessMainInfo(item, preferredCountryCode, movieInfo);

            return item;
        }

        /// <summary>
        /// Processes the main info.
        /// </summary>
        /// <param name="movie">The movie.</param>
        /// <param name="preferredCountryCode">The preferred country code.</param>
        /// <param name="movieData">The movie data.</param>
        private void ProcessMainInfo(T movie, string preferredCountryCode, MovieDbProvider.CompleteMovieData movieData)
        {
            movie.Name = movieData.title ?? movieData.original_title ?? movieData.name ?? movie.Name;

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
                var hasTagline = movie as IHasTaglines;
                if (hasTagline != null)
                {
                    hasTagline.Taglines.Clear();
                    hasTagline.AddTagline(movieData.tagline);
                }
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
                    movieItem.TmdbCollectionName = movieData.belongs_to_collection.name;
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
                var ourRelease = movieData.releases.countries.FirstOrDefault(c => c.iso_3166_1.Equals(preferredCountryCode, StringComparison.OrdinalIgnoreCase)) ?? new MovieDbProvider.Country();
                var usRelease = movieData.releases.countries.FirstOrDefault(c => c.iso_3166_1.Equals("US", StringComparison.OrdinalIgnoreCase)) ?? new MovieDbProvider.Country();
                var minimunRelease = movieData.releases.countries.OrderBy(c => c.release_date).FirstOrDefault() ?? new MovieDbProvider.Country();

                var ratingPrefix = string.Equals(preferredCountryCode, "us", StringComparison.OrdinalIgnoreCase) ? "" : preferredCountryCode + "-";
                movie.OfficialRating = !string.IsNullOrEmpty(ourRelease.certification)
                                           ? ratingPrefix + ourRelease.certification
                                           : !string.IsNullOrEmpty(usRelease.certification)
                                                 ? usRelease.certification
                                                 : !string.IsNullOrEmpty(minimunRelease.certification)
                                                       ? minimunRelease.iso_3166_1 + "-" + minimunRelease.certification
                                                       : null;
            }

            if (movieData.release_date.Year != 1)
            {
                //no specific country release info at all
                movie.PremiereDate = movieData.release_date.ToUniversalTime();
                movie.ProductionYear = movieData.release_date.Year;
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

            //Actors, Directors, Writers - all in People
            //actors come from cast
            if (movieData.casts != null && movieData.casts.cast != null)
            {
                foreach (var actor in movieData.casts.cast.OrderBy(a => a.order)) movie.AddPerson(new PersonInfo { Name = actor.name.Trim(), Role = actor.character, Type = PersonType.Actor, SortOrder = actor.order });
            }

            //and the rest from crew
            if (movieData.casts != null && movieData.casts.crew != null)
            {
                foreach (var person in movieData.casts.crew) movie.AddPerson(new PersonInfo { Name = person.name.Trim(), Role = person.job, Type = person.department });
            }

            if (movieData.keywords != null && movieData.keywords.keywords != null)
            {
                var hasTags = movie as IHasKeywords;
                if (hasTags != null)
                {
                    hasTags.Keywords = movieData.keywords.keywords.Select(i => i.name).ToList();
                }
            }

            if (movieData.trailers != null && movieData.trailers.youtube != null &&
                movieData.trailers.youtube.Count > 0)
            {
                var hasTrailers = movie as IHasTrailers;
                if (hasTrailers != null)
                {
                    hasTrailers.RemoteTrailers = movieData.trailers.youtube.Select(i => new MediaUrl
                    {
                        Url = string.Format("http://www.youtube.com/watch?v={0}", i.source),
                        IsDirectLink = false,
                        Name = i.name,
                        VideoSize = string.Equals("hd", i.size, StringComparison.OrdinalIgnoreCase) ? VideoSize.HighDefinition : VideoSize.StandardDefinition

                    }).ToList();
                }
            }
        }

    }
}
