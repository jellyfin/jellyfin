using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb
{
    public static class TmdbUtils
    {
        public const string BaseMovieDbUrl = "https://www.themoviedb.org/";
        public const string ProviderName = "TheMovieDb";
        public const string ApiKey = "4219e299c89411838049ab0dab19ebd5";
        public const string AcceptHeader = "application/json,image/*";

        public static string MapCrewToPersonType(Crew crew)
        {
            if (crew.department.Equals("production", StringComparison.InvariantCultureIgnoreCase)
                && crew.job.IndexOf("producer", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return PersonType.Producer;
            }

            if (crew.department.Equals("writing", StringComparison.InvariantCultureIgnoreCase))
            {
                return PersonType.Writer;
            }

            return null;
        }
    }
}
