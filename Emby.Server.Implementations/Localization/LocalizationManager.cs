using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Localization
{
    /// <summary>
    /// Class LocalizationManager.
    /// </summary>
    public class LocalizationManager : ILocalizationManager
    {
        private const string DefaultCulture = "en-US";
        private const string RatingsPath = "Emby.Server.Implementations.Localization.Ratings.";
        private const string CulturesPath = "Emby.Server.Implementations.Localization.iso6392.txt";
        private const string CountriesPath = "Emby.Server.Implementations.Localization.countries.json";
        private const string CoreResourcePrefix = "Emby.Server.Implementations.Localization.Core.";
        private static readonly Assembly _assembly = typeof(LocalizationManager).Assembly;
        private static readonly string[] _unratedValues = ["n/a", "unrated", "not rated", "nr"];

        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger<LocalizationManager> _logger;

        private readonly Dictionary<string, Dictionary<string, ParentalRatingScore?>> _allParentalRatings = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cultureOnlyDictionaries = new(StringComparer.OrdinalIgnoreCase);

        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        private readonly ConcurrentDictionary<string, CultureDto?> _cultureCache = new(StringComparer.OrdinalIgnoreCase);
        private List<CultureDto> _cultures = [];

        private static readonly (IReadOnlyList<LocalizationOption> Options, FrozenDictionary<string, string> Bcp47ToJellyfinMap) _localizationData = BuildLocalizationData();
        private static readonly IReadOnlyList<LocalizationOption> _localizationOptions = _localizationData.Options;

        // Maps BCP-47 hyphenated culture codes (set by ASP.NET Core's RequestLocalizationMiddleware
        // and used as CurrentUICulture.Name) to Jellyfin's underscore-based resource file codes.
        // Built reflexively from the resource file scan so both directions stay in sync.
        private static readonly FrozenDictionary<string, string> _bcp47ToJellyfinMap = _localizationData.Bcp47ToJellyfinMap;

        private FrozenDictionary<string, string> _iso6392BtoT = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationManager" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        public LocalizationManager(
            IServerConfigurationManager configurationManager,
            ILogger<LocalizationManager> logger)
        {
            _configurationManager = configurationManager;
            _logger = logger;

            _configurationManager.ConfigurationUpdated += OnConfigurationUpdated;
        }

        /// <summary>
        /// Gets the supported UI cultures.
        /// </summary>
        /// <returns>A list of <see cref="CultureInfo"/> objects covering every embedded translation.</returns>
        public static IList<CultureInfo> GetSupportedUICultures()
        {
            var cultures = new List<CultureInfo>();
            foreach (var option in _localizationOptions)
            {
                // Skip novelty codes (e.g. "pr" Pirate, "jbo" Lojban) that .NET cannot resolve.
                if (TryGetCultureInfo(option.Value, out var cultureInfo))
                {
                    cultures.Add(cultureInfo);
                }
            }

            return cultures;
        }

        /// <summary>
        /// Resolves a Jellyfin resource culture code (which may use underscores, e.g. <c>es_419</c>)
        /// to a <see cref="CultureInfo"/>. Returns <see langword="false"/> for codes .NET cannot resolve.
        /// </summary>
        private static bool TryGetCultureInfo(string cultureCode, [NotNullWhen(true)] out CultureInfo? cultureInfo)
        {
            try
            {
                // Resource files use underscores for some variants (e.g. es_419);
                // CultureInfo only accepts hyphenated BCP-47 codes.
                cultureInfo = CultureInfo.GetCultureInfo(cultureCode.Replace('_', '-'));
                return true;
            }
            catch (CultureNotFoundException)
            {
                cultureInfo = null;
                return false;
            }
        }

        private static void OnConfigurationUpdated(object? sender, EventArgs e)
        {
            if (sender is IServerConfigurationManager configManager)
            {
                var uiCulture = configManager.Configuration.UICulture;
                if (!string.IsNullOrEmpty(uiCulture))
                {
                    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(uiCulture);
                }
            }
        }

        /// <summary>
        /// Loads all resources into memory.
        /// </summary>
        /// <returns><see cref="Task" />.</returns>
        public async Task LoadAll()
        {
            // Extract from the assembly
            foreach (var resource in _assembly.GetManifestResourceNames())
            {
                if (!resource.StartsWith(RatingsPath, StringComparison.Ordinal))
                {
                    continue;
                }

                using var stream = _assembly.GetManifestResourceStream(resource);
                if (stream is not null)
                {
                    var ratingSystem = await JsonSerializer.DeserializeAsync<ParentalRatingSystem>(stream, _jsonOptions).ConfigureAwait(false)
                                ?? throw new InvalidOperationException($"Invalid resource path: '{CountriesPath}'");

                    var dict = new Dictionary<string, ParentalRatingScore?>();
                    if (ratingSystem.Ratings is not null)
                    {
                        foreach (var ratingEntry in ratingSystem.Ratings)
                        {
                            foreach (var ratingString in ratingEntry.RatingStrings)
                            {
                                dict[ratingString] = ratingEntry.RatingScore;
                            }
                        }

                        _allParentalRatings[ratingSystem.CountryCode] = dict;
                    }
                }
            }

            await LoadCultures().ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the cultures.
        /// </summary>
        /// <returns><see cref="IEnumerable{CultureDto}" />.</returns>
        public IEnumerable<CultureDto> GetCultures()
            => _cultures;

        private async Task LoadCultures()
        {
            List<CultureDto> list = [];
            Dictionary<string, string> iso6392BtoTdict = new Dictionary<string, string>();

            using var stream = _assembly.GetManifestResourceStream(CulturesPath);
            if (stream is null)
            {
                throw new InvalidOperationException($"Invalid resource path: '{CulturesPath}'");
            }
            else
            {
                using var reader = new StreamReader(stream);
                await foreach (var line in reader.ReadAllLinesAsync().ConfigureAwait(false))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var parts = line.Split('|');
                    if (parts.Length != 5)
                    {
                        throw new InvalidDataException($"Invalid culture data found at: '{line}'");
                    }

                    string name = parts[3];
                    string displayname = parts[3];
                    if (string.IsNullOrWhiteSpace(displayname))
                    {
                        continue;
                    }

                    string twoCharName = parts[2];
                    if (string.IsNullOrWhiteSpace(twoCharName))
                    {
                        twoCharName = string.Empty;
                    }
                    else if (twoCharName.Contains('-', StringComparison.OrdinalIgnoreCase))
                    {
                        name = twoCharName;
                    }

                    string[] threeLetterNames;
                    if (string.IsNullOrWhiteSpace(parts[1]))
                    {
                        threeLetterNames = [parts[0]];
                    }
                    else
                    {
                        threeLetterNames = [parts[0], parts[1]];

                        // In cases where there are two TLN the first one is ISO 639-2/T and the second one is ISO 639-2/B
                        // We need ISO 639-2/T for the .NET cultures so we cultivate a dictionary for the translation B->T
                        iso6392BtoTdict.TryAdd(parts[1], parts[0]);
                    }

                    list.Add(new CultureDto(name, displayname, twoCharName, threeLetterNames));
                }

                _cultureCache.Clear();
                _cultures = list;
                _iso6392BtoT = iso6392BtoTdict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <inheritdoc />
        public CultureDto? FindLanguageInfo(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return null;
            }

            return _cultureCache.GetOrAdd(
                language,
                static (lang, cultures) =>
                {
                    // TODO language should ideally be a ReadOnlySpan but moq cannot mock ref structs
                    for (var i = 0; i < cultures.Count; i++)
                    {
                        var culture = cultures[i];
                        if (lang.Equals(culture.DisplayName, StringComparison.OrdinalIgnoreCase)
                            || lang.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)
                            || culture.ThreeLetterISOLanguageNames.Contains(lang, StringComparison.OrdinalIgnoreCase)
                            || lang.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                        {
                            return culture;
                        }
                    }

                    return null;
                },
                _cultures);
        }

        /// <inheritdoc />
        public IReadOnlyList<CountryInfo> GetCountries()
        {
            using var stream = _assembly.GetManifestResourceStream(CountriesPath) ?? throw new InvalidOperationException($"Invalid resource path: '{CountriesPath}'");

            return JsonSerializer.Deserialize<IReadOnlyList<CountryInfo>>(stream, _jsonOptions) ?? [];
        }

        /// <inheritdoc />
        public IReadOnlyList<ParentalRating> GetParentalRatings()
        {
            // Use server default language for ratings
            // Fall back to empty list if there are no parental ratings for that language
            var ratings = GetParentalRatingsDictionary()?.Select(x => new ParentalRating(x.Key, x.Value)).ToList() ?? [];

            // Add common ratings to ensure them being available for selection
            // Based on the US rating system due to it being the main source of rating in the metadata providers
            // Unrated
            if (!ratings.Any(x => x is null))
            {
                ratings.Add(new("Unrated", null));
            }

            // Minimum rating possible
            if (ratings.All(x => x.RatingScore?.Score != 0))
            {
                ratings.Add(new("Approved", new(0, null)));
            }

            // Matches PG (this has different age restrictions depending on country)
            if (ratings.All(x => x.RatingScore?.Score != 10))
            {
                ratings.Add(new("10", new(10, null)));
            }

            // Matches PG-13
            if (ratings.All(x => x.RatingScore?.Score != 13))
            {
                ratings.Add(new("13", new(13, null)));
            }

            // Matches TV-14
            if (ratings.All(x => x.RatingScore?.Score != 14))
            {
                ratings.Add(new("14", new(14, null)));
            }

            // Catchall if max rating of country is less than 21
            // Using 21 instead of 18 to be sure to allow access to all rated content except adult and banned
            if (!ratings.Any(x => x.RatingScore?.Score >= 21))
            {
                ratings.Add(new ParentalRating("21", new(21, null)));
            }

            // A lot of countries don't explicitly have a separate rating for adult content
            if (ratings.All(x => x.RatingScore?.Score != 1000))
            {
                ratings.Add(new ParentalRating("XXX",  new(1000, null)));
            }

            // A lot of countries don't explicitly have a separate rating for banned content
            if (ratings.All(x => x.RatingScore?.Score != 1001))
            {
                ratings.Add(new ParentalRating("Banned",  new(1001, null)));
            }

            return [.. ratings.OrderBy(r => r.RatingScore?.Score).ThenBy(r => r.RatingScore?.SubScore)];
        }

        /// <summary>
        /// Gets the parental ratings dictionary.
        /// </summary>
        /// <param name="countryCode">The optional two letter ISO language string.</param>
        /// <returns><see cref="Dictionary{String, ParentalRatingScore}" />.</returns>
        private Dictionary<string, ParentalRatingScore?>? GetParentalRatingsDictionary(string? countryCode = null)
        {
            // Fallback to server default if no country code is specified.
            if (string.IsNullOrEmpty(countryCode))
            {
                countryCode = _configurationManager.Configuration.MetadataCountryCode;
            }

            if (_allParentalRatings.TryGetValue(countryCode, out var countryValue))
            {
                return countryValue;
            }

            return null;
        }

        /// <inheritdoc />
        public ParentalRatingScore? GetRatingScore(string rating, string? countryCode = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(rating);

            // Handle unrated content
            if (_unratedValues.Contains(rating.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Convert ints directly
            // This may override some of the locale specific age ratings (but those always map to the same age)
            if (int.TryParse(rating, out var ratingAge))
            {
                return new(ratingAge, null);
            }

            // Fairly common for some users to have "Rated R" in their rating field
            rating = rating.Replace("Rated :", string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Replace("Rated:", string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Replace("Rated ", string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Trim();

            // Use rating system matching the language
            if (!string.IsNullOrEmpty(countryCode))
            {
                var ratingsDictionary = GetParentalRatingsDictionary(countryCode);
                if (ratingsDictionary is not null && ratingsDictionary.TryGetValue(rating, out ParentalRatingScore? value))
                {
                    return value;
                }

                if (ratingsDictionary is not null && rating.Length > countryCode.Length
                    && rating.StartsWith(countryCode, StringComparison.OrdinalIgnoreCase)
                    && (rating[countryCode.Length] == '-' || rating[countryCode.Length] == ':')
                    && ratingsDictionary.TryGetValue(rating[(countryCode.Length + 1)..].Trim(), out var normalizedValue))
                {
                    return normalizedValue;
                }
            }
            else
            {
                // Fall back to server default language for ratings check
                var ratingsDictionary = GetParentalRatingsDictionary();
                if (ratingsDictionary is not null && ratingsDictionary.TryGetValue(rating, out ParentalRatingScore? value))
                {
                    return value;
                }
            }

            // If we don't find anything, check all ratings systems, starting with US
            if (_allParentalRatings.TryGetValue("us", out var usRatings) && usRatings.TryGetValue(rating, out var usValue))
            {
                return usValue;
            }

            foreach (var dictionary in _allParentalRatings.Values)
            {
                if (dictionary.TryGetValue(rating, out var value))
                {
                    return value;
                }
            }

            // Try splitting by country prefix separator to handle "US:PG-13", "Germany: FSK-18", "DE-FSK-18"
            if (TryGetRatingScoreBySeparator(rating, ':', out var result)
                || TryGetRatingScoreBySeparator(rating, '-', out result))
            {
                return result;
            }

            return null;
        }

        private bool TryGetRatingScoreBySeparator(string rating, char separator, out ParentalRatingScore? result)
        {
            result = null;

            if (rating.IndexOf(separator, StringComparison.Ordinal) < 0)
            {
                return false;
            }

            var ratingSpan = rating.AsSpan();
            var countryPart = ratingSpan.LeftPart(separator).Trim().ToString();
            var ratingPart = ratingSpan.RightPart(separator).Trim().ToString();
            if (ratingPart.Length == 0)
            {
                return false;
            }

            string? resolvedCountryCode = null;

            if (_allParentalRatings.ContainsKey(countryPart))
            {
                resolvedCountryCode = countryPart;
            }
            else
            {
                var culture = FindLanguageInfo(countryPart);
                if (culture is not null)
                {
                    resolvedCountryCode = culture.TwoLetterISOLanguageName;
                }
            }

            if (resolvedCountryCode is not null
                && _allParentalRatings.TryGetValue(resolvedCountryCode, out var countryRatings))
            {
                if (countryRatings.TryGetValue(ratingPart, out result))
                {
                    return true;
                }

                _logger.LogWarning(
                    "Rating '{Rating}' not found in the '{CountryCode}' rating system, treating as unrated",
                    rating,
                    resolvedCountryCode);

                return true;
            }

            // Country not identified or no rating data available, try recursive lookup
            result = GetRatingScore(ratingPart, resolvedCountryCode);

            return true;
        }

        /// <inheritdoc />
        public string GetLocalizedString(string phrase)
        {
            return GetLocalizedString(phrase, CultureInfo.CurrentUICulture.Name);
        }

        /// <inheritdoc />
        public string GetServerLocalizedString(string phrase)
        {
            return GetLocalizedString(phrase, _configurationManager.Configuration.UICulture);
        }

        /// <inheritdoc />
        public string GetLocalizedString(string phrase, string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                culture = _configurationManager.Configuration.UICulture;
            }

            if (string.IsNullOrEmpty(culture))
            {
                culture = DefaultCulture;
            }

            // Normalize BCP-47 hyphenated codes to Jellyfin's underscore-based codes
            if (_bcp47ToJellyfinMap.TryGetValue(culture, out var mapped))
            {
                culture = mapped;
            }

            var dictionary = GetLocalizationDictionary(culture);

            if (dictionary.TryGetValue(phrase, out var value))
            {
                return value;
            }

            if (!string.Equals(culture, DefaultCulture, StringComparison.OrdinalIgnoreCase))
            {
                var fallback = GetLocalizationDictionary(DefaultCulture);
                if (fallback.TryGetValue(phrase, out var fallbackValue))
                {
                    return fallbackValue;
                }
            }

            return phrase;
        }

        private Dictionary<string, string> GetLocalizationDictionary(string culture)
        {
            ArgumentException.ThrowIfNullOrEmpty(culture);

            return _cultureOnlyDictionaries.GetOrAdd(
                culture,
                static (key, localizationManager) =>
                {
                    var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var namespaceName = localizationManager.GetType().Namespace + ".Core";
                    localizationManager.CopyInto(dictionary, namespaceName + "." + GetResourceFilename(key)).GetAwaiter().GetResult();

                    return dictionary;
                },
                this);
        }

        private async Task CopyInto(IDictionary<string, string> dictionary, string resourcePath)
        {
            using var stream = _assembly.GetManifestResourceStream(resourcePath);
            // If a Culture doesn't have a translation the stream will be null and it defaults to en-us further up the chain
            if (stream is null)
            {
                _logger.LogError("Missing translation/culture resource: {ResourcePath}", resourcePath);
                return;
            }

            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, _jsonOptions).ConfigureAwait(false) ?? throw new InvalidOperationException($"Resource contains invalid data: '{stream}'");
            foreach (var key in dict.Keys)
            {
                dictionary[key] = dict[key];
            }
        }

        private static string GetResourceFilename(string culture)
        {
            var parts = culture.Split('-');

            if (parts.Length == 2)
            {
                culture = parts[0].ToLowerInvariant() + "-" + parts[1].ToUpperInvariant();
            }
            else
            {
                culture = culture.ToLowerInvariant();
            }

            return culture + ".json";
        }

        /// <inheritdoc />
        public IEnumerable<LocalizationOption> GetLocalizationOptions()
        {
            return _localizationOptions;
        }

        private static (IReadOnlyList<LocalizationOption> Options, FrozenDictionary<string, string> Bcp47ToJellyfinMap) BuildLocalizationData()
        {
            var options = new List<LocalizationOption>();
            var bcp47Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var prefix = CoreResourcePrefix;

            foreach (var resource in _assembly.GetManifestResourceNames())
            {
                if (!resource.StartsWith(prefix, StringComparison.Ordinal)
                    || !resource.EndsWith(".json", StringComparison.Ordinal))
                {
                    continue;
                }

                // Extract culture code from resource name: "...Core.de.json" -> "de", "...Core.pt-BR.json" -> "pt-BR"
                var code = resource[prefix.Length..^5];

                // Record the BCP-47 → Jellyfin mapping for any resource file using underscores.
                if (code.Contains('_', StringComparison.Ordinal))
                {
                    bcp47Map[code.Replace('_', '-')] = code;
                }

                // Skip the base language file — en-US is added explicitly below
                if (code.Equals(DefaultCulture, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var displayName = GetDisplayName(code);
                options.Add(new LocalizationOption(displayName, code));
            }

            // Ensure en-US is always present
            options.Add(new LocalizationOption("English", DefaultCulture));

            options.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return (options, bcp47Map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
        }

        private static string GetDisplayName(string cultureCode)
        {
            // Custom/novelty codes like "pr" (Pirate) — fall back to code itself
            return TryGetCultureInfo(cultureCode, out var cultureInfo)
                ? cultureInfo.NativeName
                : cultureCode;
        }

        /// <inheritdoc />
        public bool TryGetISO6392TFromB(string isoB, [NotNullWhen(true)] out string? isoT)
        {
            // Unlikely case the dictionary is not (yet) initialized properly
            if (_iso6392BtoT is null)
            {
                isoT = null;
                return false;
            }

            var result = _iso6392BtoT.TryGetValue(isoB, out isoT) && !string.IsNullOrEmpty(isoT);

            // Ensure the ISO code being null if the result is false
            if (!result)
            {
                isoT = null;
            }

            return result;
        }
    }
}
