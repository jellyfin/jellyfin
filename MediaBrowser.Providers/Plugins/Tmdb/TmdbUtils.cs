using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using TMDbLib.Objects.General;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// Utilities for the TMDb provider.
    /// </summary>
    public static partial class TmdbUtils
    {
        /// <summary>
        /// URL of the TMDb instance to use.
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
        /// The crew types to keep.
        /// </summary>
        public static readonly string[] WantedCrewTypes =
        {
            PersonType.Director,
            PersonType.Writer,
            PersonType.Producer
        };

        /// <summary>
        /// The crew kinds to keep.
        /// </summary>
        public static readonly PersonKind[] WantedCrewKinds =
        {
            PersonKind.Director,
            PersonKind.Writer,
            PersonKind.Producer
        };

        [GeneratedRegex(@"[\W_-[·]]+")]
        private static partial Regex NonWordRegex();

        /// <summary>
        /// Cleans the name according to TMDb requirements.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <returns>The cleaned name.</returns>
        public static string CleanName(string name)
        {
            // TMDb expects a space separated list of words make sure that is the case
            return NonWordRegex().Replace(name, " ");
        }

        /// <summary>
        /// Maps the TMDb provided roles for crew members to Jellyfin roles.
        /// </summary>
        /// <param name="crew">Crew member to map against the Jellyfin person types.</param>
        /// <returns>The Jellyfin person type.</returns>
        public static PersonKind MapCrewToPersonType(Crew crew)
        {
            if (string.Equals(crew.Department, "directing", StringComparison.OrdinalIgnoreCase)
                && string.Equals(crew.Job, "director", StringComparison.OrdinalIgnoreCase))
            {
                return PersonKind.Director;
            }

            if (string.Equals(crew.Department, "production", StringComparison.OrdinalIgnoreCase)
                && string.Equals(crew.Job, "producer", StringComparison.OrdinalIgnoreCase))
            {
                return PersonKind.Producer;
            }

            if (string.Equals(crew.Department, "writing", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(crew.Job, "writer", StringComparison.OrdinalIgnoreCase) || string.Equals(crew.Job, "screenplay", StringComparison.OrdinalIgnoreCase)))
            {
                return PersonKind.Writer;
            }

            return PersonKind.Unknown;
        }

        /// <summary>
        /// Determines whether a video is a trailer.
        /// </summary>
        /// <param name="video">The TMDb video.</param>
        /// <returns>A boolean indicating whether the video is a trailer.</returns>
        public static bool IsTrailerType(Video video)
        {
            return string.Equals(video.Site, "youtube", StringComparison.OrdinalIgnoreCase)
                   && (string.Equals(video.Type, "trailer", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(video.Type, "teaser", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalizes a language string for use with TMDb's include image language parameter.
        /// </summary>
        /// <param name="preferredLanguage">The preferred language as either a 2 letter code with or without country code.</param>
        /// <param name="countryCode">The country code, ISO 3166-1.</param>
        /// <returns>The comma separated language string.</returns>
        public static string GetImageLanguagesParam(string preferredLanguage, string? countryCode = null)
        {
            var languages = new List<string>();

            if (!string.IsNullOrEmpty(preferredLanguage))
            {
                preferredLanguage = NormalizeLanguage(preferredLanguage, countryCode);

                languages.Add(preferredLanguage);
            }

            languages.Add("null");

            // Always add English as fallback language
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
        /// <param name="countryCode">The country code.</param>
        /// <returns>The normalized language code.</returns>
        [return: NotNullIfNotNull(nameof(language))]
        public static string? NormalizeLanguage(string? language, string? countryCode = null)
        {
            if (string.IsNullOrEmpty(language))
            {
                return language;
            }

            // Handle es-419 (Latin American Spanish) by converting to regional variant
            if (string.Equals(language, "es-419", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(countryCode))
            {
                language = string.Equals(countryCode, "AR", StringComparison.OrdinalIgnoreCase)
                    ? "es-AR"
                    : "es-MX";
            }

            // TMDb requires this to be uppercase
            // Everything after the hyphen must be written in uppercase due to a way TMDb wrote their API.
            // See here: https://www.themoviedb.org/talk/5119221d760ee36c642af4ad?page=3#56e372a0c3a3685a9e0019ab
            var parts = language.Split('-');

            if (parts.Length == 2)
            {
                // TMDb doesn't support Switzerland (de-CH, it-CH or fr-CH) so use the language (de, it or fr) without country code
                if (string.Equals(parts[1], "CH", StringComparison.OrdinalIgnoreCase))
                {
                    return parts[0];
                }

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
        public static string AdjustImageLanguage(string? imageLanguage, string requestLanguage)
        {
            if (string.IsNullOrEmpty(imageLanguage))
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(requestLanguage)
                && requestLanguage.Length > 2
                && imageLanguage.Length == 2
                && requestLanguage.StartsWith(imageLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return requestLanguage;
            }

            // TMDb now returns xx for no language instead of an empty string.
            return string.Equals(imageLanguage, "xx", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : imageLanguage;
        }

        /// <summary>
        /// Combines the metadata country code and the parental rating from the API into the value we store in our database.
        /// </summary>
        /// <param name="countryCode">The ISO 3166-1 country code of the rating country.</param>
        /// <param name="ratingValue">The rating value returned by the TMDb API.</param>
        /// <returns>The combined parental rating of country code+rating value.</returns>
        public static string BuildParentalRating(string countryCode, string ratingValue)
        {
            // Exclude US because we store US values as TV-14 without the country code.
            var ratingPrefix = string.Equals(countryCode, "US", StringComparison.OrdinalIgnoreCase) ? string.Empty : countryCode + "-";
            var newRating = ratingPrefix + ratingValue;

            return newRating.Replace("DE-", "FSK-", StringComparison.OrdinalIgnoreCase);
        }
    }
}
