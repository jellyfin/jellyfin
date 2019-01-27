using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Localization
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

        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;
        private static readonly Assembly _assembly = typeof(LocalizationManager).Assembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationManager" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        public LocalizationManager(
            IServerConfigurationManager configurationManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            ILoggerFactory loggerFactory)
        {
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _logger = loggerFactory.CreateLogger(nameof(LocalizationManager));
        }

        public async Task LoadAll()
        {
            const string ratingsResource = "Emby.Server.Implementations.Ratings.";

            Directory.CreateDirectory(LocalizationPath);

            var existingFiles = GetRatingsFiles(LocalizationPath).Select(Path.GetFileName);

            // Extract from the assembly
            foreach (var resource in _assembly.GetManifestResourceNames()
                .Where(i => i.StartsWith(ratingsResource)))
            {
                string filename = "ratings-" + resource.Substring(ratingsResource.Length);

                if (!existingFiles.Contains(filename))
                {
                    using (var stream = _assembly.GetManifestResourceStream(resource))
                    {
                        string target = Path.Combine(LocalizationPath, filename);
                        _logger.LogInformation("Extracting ratings to {0}", target);

                        using (var fs = _fileSystem.GetFileStream(target, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
                        {
                            await stream.CopyToAsync(fs);
                        }
                    }
                }
            }

            foreach (var file in GetRatingsFiles(LocalizationPath))
            {
                await LoadRatings(file);
            }

            LoadAdditionalRatings();

            await LoadCultures();
        }

        private void LoadAdditionalRatings()
        {
            LoadRatings("au", new[]
            {
                new ParentalRating("AU-G", 1),
                new ParentalRating("AU-PG", 5),
                new ParentalRating("AU-M", 6),
                new ParentalRating("AU-MA15+", 7),
                new ParentalRating("AU-M15+", 8),
                new ParentalRating("AU-R18+", 9),
                new ParentalRating("AU-X18+", 10),
                new ParentalRating("AU-RC", 11)
            });

            LoadRatings("be", new[]
            {
                new ParentalRating("BE-AL", 1),
                new ParentalRating("BE-MG6", 2),
                new ParentalRating("BE-6", 3),
                new ParentalRating("BE-9", 5),
                new ParentalRating("BE-12", 6),
                new ParentalRating("BE-16", 8)
            });

            LoadRatings("de", new[]
            {
                new ParentalRating("DE-0", 1),
                new ParentalRating("FSK-0", 1),
                new ParentalRating("DE-6", 5),
                new ParentalRating("FSK-6", 5),
                new ParentalRating("DE-12", 7),
                new ParentalRating("FSK-12", 7),
                new ParentalRating("DE-16", 8),
                new ParentalRating("FSK-16", 8),
                new ParentalRating("DE-18", 9),
                new ParentalRating("FSK-18", 9)
            });

            LoadRatings("ru", new[]
            {
                new ParentalRating("RU-0+", 1),
                new ParentalRating("RU-6+", 3),
                new ParentalRating("RU-12+", 7),
                new ParentalRating("RU-16+", 9),
                new ParentalRating("RU-18+", 10)
            });
        }

        private void LoadRatings(string country, ParentalRating[] ratings)
        {
            _allParentalRatings[country] = ratings.ToDictionary(i => i.Name);
        }

        private IEnumerable<string> GetRatingsFiles(string directory)
            => _fileSystem.GetFilePaths(directory, false)
                .Where(i => string.Equals(Path.GetExtension(i), ".csv", StringComparison.OrdinalIgnoreCase))
                .Where(i => Path.GetFileName(i).StartsWith("ratings-", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the localization path.
        /// </summary>
        /// <value>The localization path.</value>
        public string LocalizationPath
            => Path.Combine(_configurationManager.ApplicationPaths.ProgramDataPath, "localization");

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

            const string path = "Emby.Server.Implementations.Localization.iso6392.txt";

            using (var stream = _assembly.GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

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
                            threeletterNames = new [] { parts[0] };
                        }
                        else
                        {
                            threeletterNames = new [] { parts[0], parts[1] };
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
                    _assembly.GetManifestResourceStream("Emby.Server.Implementations.Localization.countries.json"));

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

            var ratings = GetRatings(countryCode) ?? GetRatings("us");

            return ratings;
        }

        /// <summary>
        /// Gets the ratings.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        private Dictionary<string, ParentalRating> GetRatings(string countryCode)
        {
            _allParentalRatings.TryGetValue(countryCode, out var value);

            return value;
        }

        /// <summary>
        /// Loads the ratings.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>Dictionary{System.StringParentalRating}.</returns>
        private async Task LoadRatings(string file)
        {
            Dictionary<string, ParentalRating> dict = new Dictionary<string, ParentalRating>(StringComparer.OrdinalIgnoreCase);
            using (var str = File.OpenRead(file))
            using (var reader = new StreamReader(str))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] parts = line.Split(',');
                    if (parts.Length == 2
                        && int.TryParse(parts[1], NumberStyles.Integer, UsCulture, out var value))
                    {
                        dict.Add(parts[0], (new ParentalRating { Name = parts[0], Value = value }));
                    }
#if DEBUG
                    _logger.LogWarning("Misformed line in {Path}", file);
#endif
                }
            }

            var countryCode = Path.GetFileNameWithoutExtension(file).Split('-')[1];

            _allParentalRatings[countryCode] = dict;
        }

        private static readonly string[] _unratedValues = { "n/a", "unrated", "not rated" };

        /// <summary>
        /// Gets the rating level.
        /// </summary>
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

            await CopyInto(dictionary, namespaceName + "." + baseFilename);
            await CopyInto(dictionary, namespaceName + "." + GetResourceFilename(culture));

            return dictionary;
        }

        private async Task CopyInto(IDictionary<string, string> dictionary, string resourcePath)
        {
            using (var stream = _assembly.GetManifestResourceStream(resourcePath))
            {
                var dict = await _jsonSerializer.DeserializeFromStreamAsync<Dictionary<string, string>>(stream);

                foreach (var key in dict.Keys)
                {
                    dictionary[key] = dict[key];
                }
            }
        }

        private static string GetResourceFilename(string culture)
        {
            var parts = culture.Split('-');

            if (parts.Length == 2)
            {
                culture = parts[0].ToLower() + "-" + parts[1].ToUpper();
            }
            else
            {
                culture = culture.ToLower();
            }

            return culture + ".json";
        }

        public LocalizationOption[] GetLocalizationOptions()
            => new LocalizationOption[]
            {
                new LocalizationOption("Arabic", "ar"),
                new LocalizationOption("Belarusian (Belarus)", "be-BY"),
                new LocalizationOption("Bulgarian (Bulgaria)", "bg-BG"),
                new LocalizationOption("Catalan", "ca"),
                new LocalizationOption("Chinese Simplified", "zh-CN"),
                new LocalizationOption("Chinese Traditional", "zh-TW"),
                new LocalizationOption("Chinese Traditional (Hong Kong)", "zh-HK"),
                new LocalizationOption("Croatian", "hr"),
                new LocalizationOption("Czech", "cs"),
                new LocalizationOption("Danish", "da"),
                new LocalizationOption("Dutch", "nl"),
                new LocalizationOption("English (United Kingdom)", "en-GB"),
                new LocalizationOption("English (United States)", "en-US"),
                new LocalizationOption("Finnish", "fi"),
                new LocalizationOption("French", "fr"),
                new LocalizationOption("French (Canada)", "fr-CA"),
                new LocalizationOption("German", "de"),
                new LocalizationOption("Greek", "el"),
                new LocalizationOption("Hebrew", "he"),
                new LocalizationOption("Hindi (India)", "hi-IN"),
                new LocalizationOption("Hungarian", "hu"),
                new LocalizationOption("Indonesian", "id"),
                new LocalizationOption("Italian", "it"),
                new LocalizationOption("Japanese", "ja"),
                new LocalizationOption("Kazakh", "kk"),
                new LocalizationOption("Korean", "ko"),
                new LocalizationOption("Lithuanian", "lt-LT"),
                new LocalizationOption("Malay", "ms"),
                new LocalizationOption("Norwegian Bokmål", "nb"),
                new LocalizationOption("Persian", "fa"),
                new LocalizationOption("Polish", "pl"),
                new LocalizationOption("Portuguese (Brazil)", "pt-BR"),
                new LocalizationOption("Portuguese (Portugal)", "pt-PT"),
                new LocalizationOption("Romanian", "ro"),
                new LocalizationOption("Russian", "ru"),
                new LocalizationOption("Slovak", "sk"),
                new LocalizationOption("Slovenian (Slovenia)", "sl-SI"),
                new LocalizationOption("Spanish", "es"),
                new LocalizationOption("Spanish (Latin America)", "es-419"),
                new LocalizationOption("Spanish (Mexico)", "es-MX"),
                new LocalizationOption("Swedish", "sv"),
                new LocalizationOption("Swiss German", "gsw"),
                new LocalizationOption("Turkish", "tr"),
                new LocalizationOption("Ukrainian", "uk"),
                new LocalizationOption("Vietnamese", "vi")
            };
    }
}
