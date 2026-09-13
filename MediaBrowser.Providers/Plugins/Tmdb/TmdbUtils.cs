using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using PersonInfo = MediaBrowser.Controller.Entities.PersonInfo;

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

        private const int TitleExactScore = 8;
        private const int TitlePrefixScore = 4;
        private const int YearExactScore = 2;
        private const int YearAdjacentScore = 1;

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

        /// <summary>
        /// Writing jobs to keep.
        /// </summary>
        private static readonly FrozenSet<string> _writerJobs = new[]
        {
            "writer",
            "screenplay",
            "novel"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Everything that is not a letter, a number or a combining mark separates two search terms. The
        /// interpunct is kept because TMDb uses it inside titles such as "WALL·E", where it matches better
        /// than a space does.
        /// </summary>
        [GeneratedRegex(@"[^\p{L}\p{N}\p{M}·]+")]
        private static partial Regex NonSearchTermRegex();

        /// <summary>
        /// As <see cref="NonSearchTermRegex"/>, but the interpunct is a separator too, so a "WALL-E" folder
        /// and the "WALL·E" title TMDb returns compare equal.
        /// </summary>
        [GeneratedRegex(@"[^\p{L}\p{N}\p{M}]+")]
        private static partial Regex NonComparableRegex();

        /// <summary>
        /// Gets the TMDb id of an item, if it has one TMDb can be queried with.
        /// </summary>
        /// <param name="instance">The item.</param>
        /// <param name="tmdbId">The TMDb id.</param>
        /// <returns><c>true</c> if the item has a usable TMDb id; otherwise, <c>false</c>.</returns>
        public static bool TryGetTmdbId(this IHasProviderIds instance, out int tmdbId)
        {
            instance.TryGetProviderId(MetadataProvider.Tmdb, out var value);

            return TryParseTmdbId(value, out tmdbId);
        }

        /// <summary>
        /// Parses a TMDb id.
        /// </summary>
        /// <param name="value">The stored id.</param>
        /// <param name="tmdbId">The TMDb id.</param>
        /// <returns><c>true</c> if the value is a usable TMDb id; otherwise, <c>false</c>.</returns>
        public static bool TryParseTmdbId(string? value, out int tmdbId)
        {
            // Another provider can have filed one of its own ids under the TMDb key, e.g. an IMDb person
            // id. Reporting that as "no id" lets the caller fall back to a search and repair the id,
            // instead of throwing on every refresh of the item.
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out tmdbId) && tmdbId > 0;
        }

        /// <summary>
        /// Cleans the name according to TMDb requirements.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <returns>The cleaned name.</returns>
        public static string CleanName(string name)
        {
            // TMDb expects a space separated list of words make sure that is the case
            return NonSearchTermRegex().Replace(name, " ").Trim();
        }

        /// <summary>
        /// Reduces a title to the form used to compare a local name against a TMDb search result.
        /// </summary>
        /// <param name="title">The title to normalize.</param>
        /// <returns>The normalized title, or an empty string if there was nothing to normalize.</returns>
        public static string NormalizeTitle(string? title)
        {
            return string.IsNullOrEmpty(title)
                ? string.Empty
                : NonComparableRegex().Replace(title, " ").Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Picks the movie search result that best matches the name and year an item was looked up by.
        /// </summary>
        /// <param name="results">The search results, in the order TMDb returned them.</param>
        /// <param name="name">The parsed name of the local item.</param>
        /// <param name="year">The year of the local item, or 0 if it is unknown.</param>
        /// <returns>The best match, or <c>null</c> if there were no results.</returns>
        public static SearchMovie? FindBestMatch(IReadOnlyList<SearchMovie>? results, string? name, int year)
        {
            return FindBestMatch(
                results,
                name,
                year,
                static movie => movie.Title,
                static movie => movie.OriginalTitle,
                static movie => movie.ReleaseDate);
        }

        /// <summary>
        /// Picks the series search result that best matches the name and year an item was looked up by.
        /// </summary>
        /// <param name="results">The search results, in the order TMDb returned them.</param>
        /// <param name="name">The parsed name of the local item.</param>
        /// <param name="year">The year of the local item, or 0 if it is unknown.</param>
        /// <returns>The best match, or <c>null</c> if there were no results.</returns>
        public static SearchTv? FindBestMatch(IReadOnlyList<SearchTv>? results, string? name, int year)
        {
            return FindBestMatch(
                results,
                name,
                year,
                static series => series.Name,
                static series => series.OriginalName,
                static series => series.FirstAirDate);
        }

        /// <summary>
        /// Picks the search result that best matches the name and year an item was looked up by.
        /// </summary>
        /// <remarks>
        /// TMDb's year parameter only nudges relevance, it does not filter, so the first hit is regularly a
        /// different film or show that happens to share the title - searching for "Mulan" with year 2020
        /// returns the 1998 film first. A title that matches outranks one that does not, and the year only
        /// separates candidates that are otherwise equally good. When nothing matches at all TMDb's own
        /// ordering is kept, so a name that needs fuzzy matching, such as "A Christmas No. 1" for
        /// "A Christmas Number One", still resolves.
        /// </remarks>
        private static T? FindBestMatch<T>(
            IReadOnlyList<T>? results,
            string? name,
            int year,
            Func<T, string?> titleSelector,
            Func<T, string?> originalTitleSelector,
            Func<T, DateTime?> releaseDateSelector)
            where T : class
        {
            if (results is null || results.Count == 0)
            {
                return null;
            }

            var normalizedName = NormalizeTitle(name);
            if (normalizedName.Length == 0)
            {
                return results[0];
            }

            var best = results[0];
            var bestScore = 0;

            foreach (var result in results)
            {
                var score = Math.Max(
                        ScoreTitle(normalizedName, titleSelector(result)),
                        ScoreTitle(normalizedName, originalTitleSelector(result)))
                    + ScoreYear(year, releaseDateSelector(result)?.Year);

                // Strictly greater, so ties keep the earlier, more relevant result.
                if (score > bestScore)
                {
                    bestScore = score;
                    best = result;
                }
            }

            return best;
        }

        private static int ScoreTitle(string normalizedName, string? title)
        {
            var normalizedTitle = NormalizeTitle(title);

            if (string.Equals(normalizedName, normalizedTitle, StringComparison.Ordinal))
            {
                return TitleExactScore;
            }

            // Whole words only, otherwise "Wall" half matches "Wall Street".
            return normalizedTitle.Length > normalizedName.Length
                && normalizedTitle[normalizedName.Length] == ' '
                && normalizedTitle.StartsWith(normalizedName, StringComparison.Ordinal)
                    ? TitlePrefixScore
                    : 0;
        }

        private static int ScoreYear(int year, int? resultYear)
        {
            if (year <= 0 || resultYear is not int candidateYear)
            {
                return 0;
            }

            return Math.Abs(candidateYear - year) switch
            {
                0 => YearExactScore,
                // Regional release dates routinely straddle a new year.
                1 => YearAdjacentScore,
                _ => 0
            };
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
                && crew.Job is not null && _writerJobs.Contains(crew.Job))
            {
                return PersonKind.Writer;
            }

            return PersonKind.Unknown;
        }

        /// <summary>
        /// Maps an aggregated TMDb cast list, whose entries hold every role their member played.
        /// </summary>
        /// <param name="cast">The aggregated cast list, or <c>null</c>.</param>
        /// <param name="config">The configuration deciding how much of the cast to keep.</param>
        /// <param name="getProfileUrl">Resolves a profile path into an absolute image url.</param>
        /// <returns>One credit per role played.</returns>
        internal static IEnumerable<PersonInfo> MapAggregateCast(
            IReadOnlyList<CastAggregate>? cast,
            PluginConfiguration config,
            Func<string?, string?> getProfileUrl)
        {
            if (cast is null)
            {
                yield break;
            }

            var billed = cast
                .Where(member => !string.IsNullOrWhiteSpace(member.Name))
                .Where(member => !config.HideMissingCastMembers || !string.IsNullOrEmpty(member.ProfilePath))
                .OrderBy(member => member.Order)
                .Take(config.MaxCastMembers);

            foreach (var member in billed)
            {
                // An actor playing several characters over the run gets one aggregated entry holding
                // every role, so each of them becomes a credit of its own here. Their own billing puts
                // the character they played the longest first.
                var characters = member.Roles?
                    .Where(role => !string.IsNullOrWhiteSpace(role.Character))
                    .OrderByDescending(role => role.EpisodeCount)
                    .Select(role => role.Character!.Trim())
                    .ToArray();

                if (characters is null || characters.Length == 0)
                {
                    characters = [string.Empty];
                }

                foreach (var character in characters)
                {
                    yield return CreateCredit(member.Name!, member.Id, member.ProfilePath, member.Order, character, getProfileUrl);
                }
            }
        }

        /// <summary>
        /// Maps a TMDb cast list whose entries hold the one character their member is credited for.
        /// </summary>
        /// <param name="cast">The cast list, or <c>null</c>.</param>
        /// <param name="config">The configuration deciding how much of the cast to keep.</param>
        /// <param name="getProfileUrl">Resolves a profile path into an absolute image url.</param>
        /// <returns>One credit per cast entry.</returns>
        internal static IEnumerable<PersonInfo> MapCast(
            IReadOnlyList<Cast>? cast,
            PluginConfiguration config,
            Func<string?, string?> getProfileUrl)
        {
            if (cast is null)
            {
                yield break;
            }

            var billed = cast
                .Where(member => !string.IsNullOrWhiteSpace(member.Name))
                .Where(member => !config.HideMissingCastMembers || !string.IsNullOrEmpty(member.ProfilePath))
                .OrderBy(member => member.Order)
                .Take(config.MaxCastMembers);

            foreach (var member in billed)
            {
                yield return CreateCredit(member.Name!, member.Id, member.ProfilePath, member.Order, member.Character?.Trim() ?? string.Empty, getProfileUrl);
            }
        }

        private static PersonInfo CreateCredit(string name, int id, string? profilePath, int? order, string role, Func<string?, string?> getProfileUrl)
        {
            var personInfo = new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = PersonKind.Actor,
                SortOrder = order,
                ImageUrl = getProfileUrl(profilePath)
            };

            if (id > 0)
            {
                personInfo.SetProviderId(MetadataProvider.Tmdb, id.ToString(CultureInfo.InvariantCulture));
            }

            return personInfo;
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
