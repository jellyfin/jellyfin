using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb
{
    /// <summary>
    /// Utilities for the TMDb provider
    /// </summary>
    public static class TmdbUtils
    {
        /// <summary>
        /// URL of the TMDB instance to use.
        /// </summary>
        public const string BaseTmdbUrl = "https://www.themoviedb.org/";

        /// <summary>
        /// URL of the TMDB API instance to use.
        /// </summary>
        public const string BaseTmdbApiUrl = "https://api.themoviedb.org/";

        /// <summary>
        /// Name of the provider.
        /// </summary>
        public const string ProviderName = "TheMovieDb";

        /// <summary>
        /// API key to use when performing an API call.
        /// </summary>
        public const string ApiKey = "4219e299c89411838049ab0dab19ebd5";

        /// <summary>
        /// Value of the Accept header for requests to the provider.
        /// </summary>
        public const string AcceptHeader = "application/json,image/*";

        /// <summary>
        /// Maps the TMDB provided roles for crew members to Jellyfin roles.
        /// </summary>
        /// <param name="crew">Crew member to map against the Jellyfin person types.</param>
        /// <returns>The Jellyfin person type.</returns>
        public static string MapCrewToPersonType(Crew crew)
        {
            if (crew.Department.Equals("production", StringComparison.InvariantCultureIgnoreCase)
                && crew.Job.Contains("director", StringComparison.InvariantCultureIgnoreCase))
            {
                return PersonType.Director;
            }

            if (crew.Department.Equals("production", StringComparison.InvariantCultureIgnoreCase)
                && crew.Job.Contains("producer", StringComparison.InvariantCultureIgnoreCase))
            {
                return PersonType.Producer;
            }

            if (crew.Department.Equals("writing", StringComparison.InvariantCultureIgnoreCase))
            {
                return PersonType.Writer;
            }

            return null;
        }
    }
}
