using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private static readonly Assembly _assembly = typeof(LocalizationManager).Assembly;
        private static readonly string[] _unratedValues = { "n/a", "unrated", "not rated", "nr" };

        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger<LocalizationManager> _logger;

        private readonly Dictionary<string, Dictionary<string, ParentalRating>> _allParentalRatings =
            new Dictionary<string, Dictionary<string, ParentalRating>>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _dictionaries =
            new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        private List<CultureDto> _cultures = new List<CultureDto>();

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

                string countryCode = resource.Substring(RatingsPath.Length, 2);
                var dict = new Dictionary<string, ParentalRating>(StringComparer.OrdinalIgnoreCase);

                var stream = _assembly.GetManifestResourceStream(resource);
                await using (stream!.ConfigureAwait(false)) // shouldn't be null here, we just got the resource path from Assembly.GetManifestResourceNames()
                {
                    using var reader = new StreamReader(stream!);
                    await foreach (var line in reader.ReadAllLinesAsync().ConfigureAwait(false))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        string[] parts = line.Split(',');
                        if (parts.Length == 2
                            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                        {
                            var name = parts[0];
                            dict.Add(name, new ParentalRating(name, value));
                        }
                        else
                        {
                            _logger.LogWarning("Malformed line in ratings file for country {CountryCode}", countryCode);
                        }
                    }
                }

                _allParentalRatings[countryCode] = dict;
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
            List<CultureDto> list = new List<CultureDto>();

            await using var stream = _assembly.GetManifestResourceStream(CulturesPath)
                ?? throw new InvalidOperationException($"Invalid resource path: '{CulturesPath}'");
            using var reader = new StreamReader(stream);
            await foreach (var line in reader.ReadAllLinesAsync().ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('|');

                if (parts.Length == 5)
                {
                    string name = parts[3];
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    string twoCharName = parts[2];
                    if (string.IsNullOrWhiteSpace(twoCharName))
                    {
                        continue;
                    }

                    string[] threeletterNames;
                    if (string.IsNullOrWhiteSpace(parts[1]))
                    {
                        threeletterNames = new[] { parts[0] };
                    }
                    else
                    {
                        threeletterNames = new[] { parts[0], parts[1] };
                    }

                    list.Add(new CultureDto(name, name, twoCharName, threeletterNames));
                }
            }

            _cultures = list;
        }

        /// <inheritdoc />
        public CultureDto? FindLanguageInfo(string language)
        {
            // TODO language should ideally be a ReadOnlySpan but moq cannot mock ref structs
            for (var i = 0; i < _cultures.Count; i++)
            {
                var culture = _cultures[i];
                if (language.Equals(culture.DisplayName, StringComparison.OrdinalIgnoreCase)
                    || language.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)
                    || culture.ThreeLetterISOLanguageNames.Contains(language, StringComparison.OrdinalIgnoreCase)
                    || language.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                {
                    return culture;
                }
            }

            return default;
        }

        /// <inheritdoc />
        public IEnumerable<CountryInfo> GetCountries()
        {
            using StreamReader reader = new StreamReader(
                _assembly.GetManifestResourceStream(CountriesPath) ?? throw new InvalidOperationException($"Invalid resource path: '{CountriesPath}'"));
            return JsonSerializer.Deserialize<IEnumerable<CountryInfo>>(reader.ReadToEnd(), _jsonOptions)
                ?? throw new InvalidOperationException($"Resource contains invalid data: '{CountriesPath}'");
        }

        /// <inheritdoc />
        public IEnumerable<ParentalRating> GetParentalRatings()
        {
            // Use server default language for ratings
            // Fall back to empty list if there are no parental ratings for that language
            var ratings = GetParentalRatingsDictionary()?.Values.ToList()
                ?? new List<ParentalRating>();

            // Add common ratings to ensure them being available for selection
            // Based on the US rating system due to it being the main source of rating in the metadata providers
            // Unrated
            if (!ratings.Any(x => x.Value is null))
            {
                ratings.Add(new ParentalRating("Unrated", null));
            }

            // Minimum rating possible
            if (ratings.All(x => x.Value != 0))
            {
                ratings.Add(new ParentalRating("Approved", 0));
            }

            // Matches PG (this has different age restrictions depending on country)
            if (ratings.All(x => x.Value != 10))
            {
                ratings.Add(new ParentalRating("10", 10));
            }

            // Matches PG-13
            if (ratings.All(x => x.Value != 13))
            {
                ratings.Add(new ParentalRating("13", 13));
            }

            // Matches TV-14
            if (ratings.All(x => x.Value != 14))
            {
                ratings.Add(new ParentalRating("14", 14));
            }

            // Catchall if max rating of country is less than 21
            // Using 21 instead of 18 to be sure to allow access to all rated content except adult and banned
            if (!ratings.Any(x => x.Value >= 21))
            {
                ratings.Add(new ParentalRating("21", 21));
            }

            // A lot of countries don't excplicitly have a seperate rating for adult content
            if (ratings.All(x => x.Value != 1000))
            {
                ratings.Add(new ParentalRating("XXX", 1000));
            }

            // A lot of countries don't excplicitly have a seperate rating for banned content
            if (ratings.All(x => x.Value != 1001))
            {
                ratings.Add(new ParentalRating("Banned", 1001));
            }

            return ratings.OrderBy(r => r.Value);
        }

        /// <summary>
        /// Gets the parental ratings dictionary.
        /// </summary>
        /// <param name="countryCode">The optional two letter ISO language string.</param>
        /// <returns><see cref="Dictionary{String, ParentalRating}" />.</returns>
        private Dictionary<string, ParentalRating>? GetParentalRatingsDictionary(string? countryCode = null)
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
        public int? GetRatingLevel(string rating, string? countryCode = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(rating);

            // Handle unrated content
            if (_unratedValues.Contains(rating.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Convert integers directly
            // This may override some of the locale specific age ratings (but those always map to the same age)
            if (int.TryParse(rating, out var ratingAge))
            {
                return ratingAge;
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
                if (ratingsDictionary is not null && ratingsDictionary.TryGetValue(rating, out ParentalRating? value))
                {
                    return value.Value;
                }
            }
            else
            {
                // Fall back to server default language for ratings check
                // If it has no ratings, use the US ratings
                var ratingsDictionary = GetParentalRatingsDictionary() ?? GetParentalRatingsDictionary("us");
                if (ratingsDictionary is not null && ratingsDictionary.TryGetValue(rating, out ParentalRating? value))
                {
                    return value.Value;
                }
            }

            // If we don't find anything, check all ratings systems
            foreach (var dictionary in _allParentalRatings.Values)
            {
                if (dictionary.TryGetValue(rating, out var value))
                {
                    return value.Value;
                }
            }

            // Try splitting by : to handle "Germany: FSK-18"
            if (rating.Contains(':', StringComparison.OrdinalIgnoreCase))
            {
                var ratingLevelRightPart = rating.AsSpan().RightPart(':');
                if (ratingLevelRightPart.Length != 0)
                {
                    return GetRatingLevel(ratingLevelRightPart.ToString());
                }
            }

            // Handle prefix country code to handle "DE-18"
            if (rating.Contains('-', StringComparison.OrdinalIgnoreCase))
            {
                var ratingSpan = rating.AsSpan();

                // Extract culture from country prefix
                var culture = FindLanguageInfo(ratingSpan.LeftPart('-').ToString());

                var ratingLevelRightPart = ratingSpan.RightPart('-');
                if (ratingLevelRightPart.Length != 0)
                {
                    // Check rating system of culture
                    return GetRatingLevel(ratingLevelRightPart.ToString(), culture?.TwoLetterISOLanguageName);
                }
            }

            return null;
        }

        /// <inheritdoc />
        public string GetLocalizedString(string phrase)
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

            var dictionary = GetLocalizationDictionary(culture);

            if (dictionary.TryGetValue(phrase, out var value))
            {
                return value;
            }

            return phrase;
        }

        private Dictionary<string, string> GetLocalizationDictionary(string culture)
        {
            ArgumentException.ThrowIfNullOrEmpty(culture);

            const string Prefix = "Core";

            return _dictionaries.GetOrAdd(
                culture,
                static (key, localizationManager) => localizationManager.GetDictionary(Prefix, key, DefaultCulture + ".json").GetAwaiter().GetResult(),
                this);
        }

        private async Task<Dictionary<string, string>> GetDictionary(string prefix, string culture, string baseFilename)
        {
            ArgumentException.ThrowIfNullOrEmpty(culture);

            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var namespaceName = GetType().Namespace + "." + prefix;

            await CopyInto(dictionary, namespaceName + "." + baseFilename).ConfigureAwait(false);
            await CopyInto(dictionary, namespaceName + "." + GetResourceFilename(culture)).ConfigureAwait(false);

            return dictionary;
        }

        private async Task CopyInto(IDictionary<string, string> dictionary, string resourcePath)
        {
            await using var stream = _assembly.GetManifestResourceStream(resourcePath);
            // If a Culture doesn't have a translation the stream will be null and it defaults to en-us further up the chain
            if (stream is null)
            {
                _logger.LogError("Missing translation/culture resource: {ResourcePath}", resourcePath);
                return;
            }

            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, _jsonOptions).ConfigureAwait(false);
            if (dict is null)
            {
                throw new InvalidOperationException($"Resource contains invalid data: '{stream}'");
            }

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
            yield return new LocalizationOption("Afrikaans", "af");
            yield return new LocalizationOption("العربية", "ar");
            yield return new LocalizationOption("Беларуская", "be");
            yield return new LocalizationOption("Български", "bg-BG");
            yield return new LocalizationOption("বাংলা (বাংলাদেশ)", "bn");
            yield return new LocalizationOption("Català", "ca");
            yield return new LocalizationOption("Čeština", "cs");
            yield return new LocalizationOption("Cymraeg", "cy");
            yield return new LocalizationOption("Dansk", "da");
            yield return new LocalizationOption("Deutsch", "de");
            yield return new LocalizationOption("English (United Kingdom)", "en-GB");
            yield return new LocalizationOption("English", "en-US");
            yield return new LocalizationOption("Ελληνικά", "el");
            yield return new LocalizationOption("Esperanto", "eo");
            yield return new LocalizationOption("Español", "es");
            yield return new LocalizationOption("Español americano", "es_419");
            yield return new LocalizationOption("Español (Argentina)", "es-AR");
            yield return new LocalizationOption("Español (Dominicana)", "es_DO");
            yield return new LocalizationOption("Español (México)", "es-MX");
            yield return new LocalizationOption("Eesti", "et");
            yield return new LocalizationOption("Basque", "eu");
            yield return new LocalizationOption("فارسی", "fa");
            yield return new LocalizationOption("Suomi", "fi");
            yield return new LocalizationOption("Filipino", "fil");
            yield return new LocalizationOption("Français", "fr");
            yield return new LocalizationOption("Français (Canada)", "fr-CA");
            yield return new LocalizationOption("Galego", "gl");
            yield return new LocalizationOption("Schwiizerdütsch", "gsw");
            yield return new LocalizationOption("עִבְרִית", "he");
            yield return new LocalizationOption("हिन्दी", "hi");
            yield return new LocalizationOption("Hrvatski", "hr");
            yield return new LocalizationOption("Magyar", "hu");
            yield return new LocalizationOption("Bahasa Indonesia", "id");
            yield return new LocalizationOption("Íslenska", "is");
            yield return new LocalizationOption("Italiano", "it");
            yield return new LocalizationOption("日本語", "ja");
            yield return new LocalizationOption("Qazaqşa", "kk");
            yield return new LocalizationOption("한국어", "ko");
            yield return new LocalizationOption("Lietuvių", "lt");
            yield return new LocalizationOption("Latviešu", "lv");
            yield return new LocalizationOption("Македонски", "mk");
            yield return new LocalizationOption("മലയാളം", "ml");
            yield return new LocalizationOption("मराठी", "mr");
            yield return new LocalizationOption("Bahasa Melayu", "ms");
            yield return new LocalizationOption("Norsk bokmål", "nb");
            yield return new LocalizationOption("नेपाली", "ne");
            yield return new LocalizationOption("Nederlands", "nl");
            yield return new LocalizationOption("Norsk nynorsk", "nn");
            yield return new LocalizationOption("ਪੰਜਾਬੀ", "pa");
            yield return new LocalizationOption("Polski", "pl");
            yield return new LocalizationOption("Pirate", "pr");
            yield return new LocalizationOption("Português", "pt");
            yield return new LocalizationOption("Português (Brasil)", "pt-BR");
            yield return new LocalizationOption("Português (Portugal)", "pt-PT");
            yield return new LocalizationOption("Românește", "ro");
            yield return new LocalizationOption("Русский", "ru");
            yield return new LocalizationOption("Slovenčina", "sk");
            yield return new LocalizationOption("Slovenščina", "sl-SI");
            yield return new LocalizationOption("Shqip", "sq");
            yield return new LocalizationOption("Српски", "sr");
            yield return new LocalizationOption("Svenska", "sv");
            yield return new LocalizationOption("தமிழ்", "ta");
            yield return new LocalizationOption("తెలుగు", "te");
            yield return new LocalizationOption("ภาษาไทย", "th");
            yield return new LocalizationOption("Türkçe", "tr");
            yield return new LocalizationOption("Українська", "uk");
            yield return new LocalizationOption("اُردُو", "ur_PK");
            yield return new LocalizationOption("Tiếng Việt", "vi");
            yield return new LocalizationOption("汉语 (简体字)", "zh-CN");
            yield return new LocalizationOption("漢語 (繁體字)", "zh-TW");
            yield return new LocalizationOption("廣東話 (香港)", "zh-HK");
        }
    }
}
