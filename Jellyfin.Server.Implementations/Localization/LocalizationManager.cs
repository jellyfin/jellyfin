using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Controller.Configuration;
using Jellyfin.Model.Entities;
using Jellyfin.Model.Extensions;
using Jellyfin.Model.Globalization;
using Jellyfin.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Localization
{
    /// <summary>
    /// Class LocalizationManager
    /// </summary>
    public class LocalizationManager : ILocalizationManager
    {
        /// <summary>
        /// The _configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _configurationManager;

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private readonly Dictionary<string, Dictionary<string, ParentalRating>> _allParentalRatings =
            new Dictionary<string, Dictionary<string, ParentalRating>>(StringComparer.OrdinalIgnoreCase);

        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;
        private static readonly Assembly _assembly = typeof(LocalizationManager).Assembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationManager" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="loggerFactory">The logger factory</param>
        public LocalizationManager(
            IServerConfigurationManager configurationManager,
            IJsonSerializer jsonSerializer,
            ILoggerFactory loggerFactory)
        {
            _configurationManager = configurationManager;
            _jsonSerializer = jsonSerializer;
            _logger = loggerFactory.CreateLogger(nameof(LocalizationManager));
        }

        public async Task LoadAll()
        {
            const string RatingsResource = "Jellyfin.Server.Implementations.Localization.Ratings.";

            // Extract from the assembly
            foreach (var resource in _assembly.GetManifestResourceNames())
            {
                if (!resource.StartsWith(RatingsResource, StringComparison.Ordinal))
                {
                    continue;
                }

                string countryCode = resource.Substring(RatingsResource.Length, 2);
                var dict = new Dictionary<string, ParentalRating>(StringComparer.OrdinalIgnoreCase);

                using (var str = _assembly.GetManifestResourceStream(resource))
                using (var reader = new StreamReader(str))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        string[] parts = line.Split(',');
                        if (parts.Length == 2
                            && int.TryParse(parts[1], NumberStyles.Integer, UsCulture, out var value))
                        {
                            dict.Add(parts[0], new ParentalRating { Name = parts[0], Value = value });
                        }
#if DEBUG
                        else
                        {
                            _logger.LogWarning("Malformed line in ratings file for country {CountryCode}", countryCode);
                        }
#endif
                    }
                }

                _allParentalRatings[countryCode] = dict;
            }

            await LoadCultures().ConfigureAwait(false);
        }

        public string NormalizeFormKD(string text)
            => text.Normalize(NormalizationForm.FormKD);

        private CultureDto[] _cultures;

        /// <summary>
        /// Gets the cultures.
        /// </summary>
        /// <returns>IEnumerable{CultureDto}.</returns>
        public CultureDto[] GetCultures()
            => _cultures;

        private async Task LoadCultures()
        {
            List<CultureDto> list = new List<CultureDto>();

            const string ResourcePath = "Jellyfin.Server.Implementations.Localization.iso6392.txt";

            using (var stream = _assembly.GetManifestResourceStream(ResourcePath))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);

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

                        list.Add(new CultureDto
                        {
                            DisplayName = name,
                            Name = name,
                            ThreeLetterISOLanguageNames = threeletterNames,
                            TwoLetterISOLanguageName = twoCharName
                        });
                    }
                }
            }

            _cultures = list.ToArray();
        }

        public CultureDto FindLanguageInfo(string language)
            => GetCultures()
                .FirstOrDefault(i =>
                    string.Equals(i.DisplayName, language, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(i.Name, language, StringComparison.OrdinalIgnoreCase)
                    || i.ThreeLetterISOLanguageNames.Contains(language, StringComparer.OrdinalIgnoreCase)
                    || string.Equals(i.TwoLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>IEnumerable{CountryInfo}.</returns>
        public Task<CountryInfo[]> GetCountries()
            => _jsonSerializer.DeserializeFromStreamAsync<CountryInfo[]>(
                    _assembly.GetManifestResourceStream("Jellyfin.Server.Implementations.Localization.countries.json"));

        /// <summary>
        /// Gets the parental ratings.
        /// </summary>
        /// <returns>IEnumerable{ParentalRating}.</returns>
        public IEnumerable<ParentalRating> GetParentalRatings()
            => GetParentalRatingsDictionary().Values;

        /// <summary>
        /// Gets the parental ratings dictionary.
        /// </summary>
        /// <returns>Dictionary{System.StringParentalRating}.</returns>
        private Dictionary<string, ParentalRating> GetParentalRatingsDictionary()
        {
            var countryCode = _configurationManager.Configuration.MetadataCountryCode;

            if (string.IsNullOrEmpty(countryCode))
            {
                countryCode = "us";
            }

           return GetRatings(countryCode) ?? GetRatings("us");
        }

        /// <summary>
        /// Gets the ratings.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        /// <returns>The ratings</returns>
        private Dictionary<string, ParentalRating> GetRatings(string countryCode)
        {
            _allParentalRatings.TryGetValue(countryCode, out var value);

            return value;
        }

        private static readonly string[] _unratedValues = { "n/a", "unrated", "not rated" };

        /// <inheritdoc />
        /// <summary>
        /// Gets the rating level.
        /// </summary>
        /// <param name="rating">Rating field</param>
        /// <returns>The rating level</returns>&gt;
        public int? GetRatingLevel(string rating)
        {
            if (string.IsNullOrEmpty(rating))
            {
                throw new ArgumentNullException(nameof(rating));
            }

            if (_unratedValues.Contains(rating, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            // Fairly common for some users to have "Rated R" in their rating field
            rating = rating.Replace("Rated ", string.Empty, StringComparison.OrdinalIgnoreCase);

            var ratingsDictionary = GetParentalRatingsDictionary();

            if (ratingsDictionary.TryGetValue(rating, out ParentalRating value))
            {
                return value.Value;
            }

            // If we don't find anything check all ratings systems
            foreach (var dictionary in _allParentalRatings.Values)
            {
                if (dictionary.TryGetValue(rating, out value))
                {
                    return value.Value;
                }
            }

            // Try splitting by : to handle "Germany: FSK 18"
            var index = rating.IndexOf(':');
            if (index != -1)
            {
                rating = rating.Substring(index).TrimStart(':').Trim();

                if (!string.IsNullOrWhiteSpace(rating))
                {
                    return GetRatingLevel(rating);
                }
            }

            // TODO: Further improve by normalizing out all spaces and dashes
            return null;
        }

        public bool HasUnicodeCategory(string value, UnicodeCategory category)
        {
            foreach (var chr in value)
            {
                if (char.GetUnicodeCategory(chr) == category)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetLocalizedString(string phrase)
        {
            return GetLocalizedString(phrase, _configurationManager.Configuration.UICulture);
        }

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

        private const string DefaultCulture = "en-US";

        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _dictionaries =
            new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> GetLocalizationDictionary(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture));
            }

            const string prefix = "Core";
            var key = prefix + culture;

            return _dictionaries.GetOrAdd(key,
                    f => GetDictionary(prefix, culture, DefaultCulture + ".json").GetAwaiter().GetResult());
        }

        private async Task<Dictionary<string, string>> GetDictionary(string prefix, string culture, string baseFilename)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture));
            }

            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var namespaceName = GetType().Namespace + "." + prefix;

            await CopyInto(dictionary, namespaceName + "." + baseFilename).ConfigureAwait(false);
            await CopyInto(dictionary, namespaceName + "." + GetResourceFilename(culture)).ConfigureAwait(false);

            return dictionary;
        }

        private async Task CopyInto(IDictionary<string, string> dictionary, string resourcePath)
        {
            using (var stream = _assembly.GetManifestResourceStream(resourcePath))
            {
                // If a Culture doesn't have a translation the stream will be null and it defaults to en-us further up the chain
                if (stream != null)
                {
                    var dict = await _jsonSerializer.DeserializeFromStreamAsync<Dictionary<string, string>>(stream).ConfigureAwait(false);

                    foreach (var key in dict.Keys)
                    {
                        dictionary[key] = dict[key];
                    }
                }
                else
                {
                    _logger.LogError("Missing translation/culture resource: {ResourcePath}", resourcePath);
                }
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

        public LocalizationOption[] GetLocalizationOptions()
            => new LocalizationOption[]
            {
                new LocalizationOption("Arabic", "ar"),
                new LocalizationOption("Bulgarian (Bulgaria)", "bg-BG"),
                new LocalizationOption("Catalan", "ca"),
                new LocalizationOption("Chinese Simplified", "zh-CN"),
                new LocalizationOption("Chinese Traditional", "zh-TW"),
                new LocalizationOption("Croatian", "hr"),
                new LocalizationOption("Czech", "cs"),
                new LocalizationOption("Danish", "da"),
                new LocalizationOption("Dutch", "nl"),
                new LocalizationOption("English (United Kingdom)", "en-GB"),
                new LocalizationOption("English (United States)", "en-US"),
                new LocalizationOption("French", "fr"),
                new LocalizationOption("French (Canada)", "fr-CA"),
                new LocalizationOption("German", "de"),
                new LocalizationOption("Greek", "el"),
                new LocalizationOption("Hebrew", "he"),
                new LocalizationOption("Hungarian", "hu"),
                new LocalizationOption("Italian", "it"),
                new LocalizationOption("Kazakh", "kk"),
                new LocalizationOption("Korean", "ko"),
                new LocalizationOption("Lithuanian", "lt-LT"),
                new LocalizationOption("Malay", "ms"),
                new LocalizationOption("Norwegian Bokmål", "nb"),
                new LocalizationOption("Persian", "fa"),
                new LocalizationOption("Polish", "pl"),
                new LocalizationOption("Portuguese (Brazil)", "pt-BR"),
                new LocalizationOption("Portuguese (Portugal)", "pt-PT"),
                new LocalizationOption("Russian", "ru"),
                new LocalizationOption("Slovak", "sk"),
                new LocalizationOption("Slovenian (Slovenia)", "sl-SI"),
                new LocalizationOption("Spanish", "es"),
                new LocalizationOption("Spanish (Argentina)", "es-AR"),
                new LocalizationOption("Spanish (Mexico)", "es-MX"),
                new LocalizationOption("Swedish", "sv"),
                new LocalizationOption("Swiss German", "gsw"),
                new LocalizationOption("Turkish", "tr")
            };
    }
}
