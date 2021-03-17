#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Entities;
using TMDbLib.Objects.General;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// Utilities for the TMDb provider.
    /// </summary>
    public static class TmdbUtils
    {
        private static readonly Regex _nonWords = new (@"[\W_]+", RegexOptions.Compiled);

        /// <summary>
        /// URL of the TMDB instance to use.
        /// </summary>
        public const string BaseTmdbUrl = "https://www.themoviedb.org/";

        /// <summary>
        /// Name of the provider.
        /// </summary>
        public const string ProviderName = "TheMovieDb";

        /// <summary>
        /// API key to use when performing an API call.
        /// </summary>
        public const string ApiKey = "4219e299c89411838049ab0dab19ebd5";

        /// <summary>
        /// Maximum number of cast members to pull.
        /// </summary>
        public const int MaxCastMembers = 15;

        /// <summary>
        /// The crew types to keep.
        /// </summary>
        public static readonly string[] WantedCrewTypes =
        {
            PersonType.Director,
            PersonType.Writer,
            PersonType.Producer
        };

        /// <summary>
        /// Cleans the name according to TMDb requirements.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <returns>The cleaned name.</returns>
        public static string CleanName(string name)
        {
            // TMDb expects a space separated list of words make sure that is the case
            return _nonWords.Replace(name, " ");
        }

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

            return string.Empty;
        }

        /// <summary>
        /// Determines whether a video is a trailer.
        /// </summary>
        /// <param name="video">The TMDb video.</param>
        /// <returns>A boolean indicating whether the video is a trailer.</returns>
        public static bool IsTrailerType(Video video)
        {
            return video.Site.Equals("youtube", StringComparison.OrdinalIgnoreCase)
                   && (!video.Type.Equals("trailer", StringComparison.OrdinalIgnoreCase)
                       || !video.Type.Equals("teaser", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalizes a language string for use with TMDb's include image language parameter.
        /// </summary>
        /// <param name="preferredLanguage">The preferred language as either a 2 letter code with or without country code.</param>
        /// <returns>The comma separated language string.</returns>
        public static string GetImageLanguagesParam(string preferredLanguage)
        {
            var languages = new List<string>();

            if (!string.IsNullOrEmpty(preferredLanguage))
            {
                preferredLanguage = NormalizeLanguage(preferredLanguage);

                languages.Add(preferredLanguage);

                if (preferredLanguage.Length == 5) // like en-US
                {
                    // Currently, TMDB supports 2-letter language codes only
                    // They are planning to change this in the future, thus we're
                    // supplying both codes if we're having a 5-letter code.
                    languages.Add(preferredLanguage.Substring(0, 2));
                }
            }

            languages.Add("null");

            if (!string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase))
            {
                languages.Add("en");
            }

            return string.Join(',', languages);
        }

        /// <summary>
        /// Normalizes a language string for use with TMDb's language parameter.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The normalized language code.</returns>
        public static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return language;
            }

            // They require this to be uppercase
            // Everything after the hyphen must be written in uppercase due to a way TMDB wrote their api.
            // See here: https://www.themoviedb.org/talk/5119221d760ee36c642af4ad?page=3#56e372a0c3a3685a9e0019ab
            var parts = language.Split('-');

            if (parts.Length == 2)
            {
                language = parts[0] + "-" + parts[1].ToUpperInvariant();
            }

            return language;
        }

        /// <summary>
        /// Adjusts the image's language code preferring the 5 letter language code eg. en-US.
        /// </summary>
        /// <param name="imageLanguage">The image's actual language code.</param>
        /// <param name="requestLanguage">The requested language code.</param>
        /// <returns>The language code.</returns>
        public static string AdjustImageLanguage(string imageLanguage, string requestLanguage)
        {
            if (!string.IsNullOrEmpty(imageLanguage)
                && !string.IsNullOrEmpty(requestLanguage)
                && requestLanguage.Length > 2
                && imageLanguage.Length == 2
                && requestLanguage.StartsWith(imageLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return requestLanguage;
            }

            return imageLanguage;
        }
    }
}
