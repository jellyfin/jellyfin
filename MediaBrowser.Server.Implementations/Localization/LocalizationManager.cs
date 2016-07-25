using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CommonIO;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.Localization
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

        private readonly ConcurrentDictionary<string, Dictionary<string, ParentalRating>> _allParentalRatings =
            new ConcurrentDictionary<string, Dictionary<string, ParentalRating>>(StringComparer.OrdinalIgnoreCase);

        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationManager" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        public LocalizationManager(IServerConfigurationManager configurationManager, IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _logger = logger;

            ExtractAll();
        }

        private void ExtractAll()
        {
            var type = GetType();
            var resourcePath = type.Namespace + ".Ratings.";

            var localizationPath = LocalizationPath;

			_fileSystem.CreateDirectory(localizationPath);

            var existingFiles = Directory.EnumerateFiles(localizationPath, "ratings-*.txt", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .ToList();

            // Extract from the assembly
            foreach (var resource in type.Assembly
                .GetManifestResourceNames()
                .Where(i => i.StartsWith(resourcePath)))
            {
                var filename = "ratings-" + resource.Substring(resourcePath.Length);

                if (!existingFiles.Contains(filename))
                {
                    using (var stream = type.Assembly.GetManifestResourceStream(resource))
                    {
                        var target = Path.Combine(localizationPath, filename);
                        _logger.Info("Extracting ratings to {0}", target);

                        using (var fs = _fileSystem.GetFileStream(target, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            stream.CopyTo(fs);
                        }
                    }
                }
            }

            foreach (var file in Directory.EnumerateFiles(localizationPath, "ratings-*.txt", SearchOption.TopDirectoryOnly))
            {
                LoadRatings(file);
            }
        }

        /// <summary>
        /// Gets the localization path.
        /// </summary>
        /// <value>The localization path.</value>
        public string LocalizationPath
        {
            get
            {
                return Path.Combine(_configurationManager.ApplicationPaths.ProgramDataPath, "localization");
            }
        }

        /// <summary>
        /// Gets the cultures.
        /// </summary>
        /// <returns>IEnumerable{CultureDto}.</returns>
        public IEnumerable<CultureDto> GetCultures()
        {
            var type = GetType();
            var path = type.Namespace + ".iso6392.txt";

            var list = new List<CultureDto>();

            using (var stream = type.Assembly.GetManifestResourceStream(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var parts = line.Split('|');

                            if (parts.Length == 5)
                            {
                                list.Add(new CultureDto
                                {
                                    DisplayName = parts[3],
                                    Name = parts[3],
                                    ThreeLetterISOLanguageName = parts[0],
                                    TwoLetterISOLanguageName = parts[2]
                                });
                            }
                        }
                    }
                }
            }

            return list.Where(i => !string.IsNullOrWhiteSpace(i.Name) &&
                !string.IsNullOrWhiteSpace(i.DisplayName) &&
                !string.IsNullOrWhiteSpace(i.ThreeLetterISOLanguageName) &&
                !string.IsNullOrWhiteSpace(i.TwoLetterISOLanguageName));
        }

        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>IEnumerable{CountryInfo}.</returns>
        public IEnumerable<CountryInfo> GetCountries()
        {
            var type = GetType();
            var path = type.Namespace + ".countries.json";

            using (var stream = type.Assembly.GetManifestResourceStream(path))
            {
                return _jsonSerializer.DeserializeFromStream<List<CountryInfo>>(stream);
            }
        }

        /// <summary>
        /// Gets the parental ratings.
        /// </summary>
        /// <returns>IEnumerable{ParentalRating}.</returns>
        public IEnumerable<ParentalRating> GetParentalRatings()
        {
            return GetParentalRatingsDictionary().Values.ToList();
        }

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

            var ratings = GetRatings(countryCode);

            if (ratings == null)
            {
                ratings = GetRatings("us");
            }

            return ratings;
        }

        /// <summary>
        /// Gets the ratings.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        private Dictionary<string, ParentalRating> GetRatings(string countryCode)
        {
            Dictionary<string, ParentalRating> value;

            _allParentalRatings.TryGetValue(countryCode, out value);

            return value;
        }

        /// <summary>
        /// Loads the ratings.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>Dictionary{System.StringParentalRating}.</returns>
        private void LoadRatings(string file)
        {
			var dict = File.ReadAllLines(file).Select(i =>
            {
                if (!string.IsNullOrWhiteSpace(i))
                {
                    var parts = i.Split(',');

                    if (parts.Length == 2)
                    {
                        int value;

                        if (int.TryParse(parts[1], NumberStyles.Integer, UsCulture, out value))
                        {
                            return new ParentalRating { Name = parts[0], Value = value };
                        }
                    }
                }

                return null;

            })
            .Where(i => i != null)
            .ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

            var countryCode = _fileSystem.GetFileNameWithoutExtension(file)
                .Split('-')
                .Last();

            _allParentalRatings.TryAdd(countryCode, dict);
        }

        private readonly string[] _unratedValues = {"n/a", "unrated", "not rated"};

        /// <summary>
        /// Gets the rating level.
        /// </summary>
        public int? GetRatingLevel(string rating)
        {
            if (string.IsNullOrEmpty(rating))
            {
                throw new ArgumentNullException("rating");
            }

            if (_unratedValues.Contains(rating, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            // Fairly common for some users to have "Rated R" in their rating field
            rating = rating.Replace("Rated ", string.Empty, StringComparison.OrdinalIgnoreCase);

            var ratingsDictionary = GetParentalRatingsDictionary();

            ParentalRating value;

            if (!ratingsDictionary.TryGetValue(rating, out value))
            {
                // If we don't find anything check all ratings systems
                foreach (var dictionary in _allParentalRatings.Values)
                {
                    if (dictionary.TryGetValue(rating, out value))
                    {
                        return value.Value;
                    }
                }
            }

            return value == null ? (int?)null : value.Value;
        }

        public string GetLocalizedString(string phrase)
        {
            return GetLocalizedString(phrase, _configurationManager.Configuration.UICulture);
        }

        public string GetLocalizedString(string phrase, string culture)
        {
            var dictionary = GetLocalizationDictionary(culture);

            string value;

            if (dictionary.TryGetValue(phrase, out value))
            {
                return value;
            }

            return phrase;
        }

        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _dictionaries =
            new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> GetLocalizationDictionary(string culture)
        {
            const string prefix = "Core";
            var key = prefix + culture;

            return _dictionaries.GetOrAdd(key, k => GetDictionary(prefix, culture, "core.json"));
        }

        private Dictionary<string, string> GetDictionary(string prefix, string culture, string baseFilename)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var assembly = GetType().Assembly;
            var namespaceName = GetType().Namespace + "." + prefix;

            CopyInto(dictionary, namespaceName + "." + baseFilename, assembly);
            CopyInto(dictionary, namespaceName + "." + GetResourceFilename(culture), assembly);

            return dictionary;
        }

        private void CopyInto(IDictionary<string, string> dictionary, string resourcePath, Assembly assembly)
        {
            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    var dict = _jsonSerializer.DeserializeFromStream<Dictionary<string, string>>(stream);

                    foreach (var key in dict.Keys)
                    {
                        dictionary[key] = dict[key];
                    }
                }
            }
        }

        private string GetResourceFilename(string culture)
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

        public IEnumerable<LocalizatonOption> GetLocalizationOptions()
        {
            return new List<LocalizatonOption>
            {
                new LocalizatonOption{ Name="Arabic", Value="ar"},
                new LocalizatonOption{ Name="Bulgarian (Bulgaria)", Value="bg-BG"},
                new LocalizatonOption{ Name="Catalan", Value="ca"},
                new LocalizatonOption{ Name="Chinese Simplified", Value="zh-CN"},
                new LocalizatonOption{ Name="Chinese Traditional", Value="zh-TW"},
                new LocalizatonOption{ Name="Croatian", Value="hr"},
                new LocalizatonOption{ Name="Czech", Value="cs"},
                new LocalizatonOption{ Name="Danish", Value="da"},
                new LocalizatonOption{ Name="Dutch", Value="nl"},
                new LocalizatonOption{ Name="English (United Kingdom)", Value="en-GB"},
                new LocalizatonOption{ Name="English (United States)", Value="en-us"},
                new LocalizatonOption{ Name="Finnish", Value="fi"},
                new LocalizatonOption{ Name="French", Value="fr"},
                new LocalizatonOption{ Name="French (Canada)", Value="fr-CA"},
                new LocalizatonOption{ Name="German", Value="de"},
                new LocalizatonOption{ Name="Greek", Value="el"},
                new LocalizatonOption{ Name="Hebrew", Value="he"},
                new LocalizatonOption{ Name="Hungarian", Value="hu"},
                new LocalizatonOption{ Name="Indonesian", Value="id"},
                new LocalizatonOption{ Name="Italian", Value="it"},
                new LocalizatonOption{ Name="Kazakh", Value="kk"},
                new LocalizatonOption{ Name="Norwegian Bokmål", Value="nb"},
                new LocalizatonOption{ Name="Polish", Value="pl"},
                new LocalizatonOption{ Name="Portuguese (Brazil)", Value="pt-BR"},
                new LocalizatonOption{ Name="Portuguese (Portugal)", Value="pt-PT"},
                new LocalizatonOption{ Name="Russian", Value="ru"},
                new LocalizatonOption{ Name="Slovenian (Slovenia)", Value="sl-SI"},
                new LocalizatonOption{ Name="Spanish", Value="es-ES"},
                new LocalizatonOption{ Name="Spanish (Mexico)", Value="es-MX"},
                new LocalizatonOption{ Name="Swedish", Value="sv"},
                new LocalizatonOption{ Name="Turkish", Value="tr"},
                new LocalizatonOption{ Name="Ukrainian", Value="uk"},
                new LocalizatonOption{ Name="Vietnamese", Value="vi"}

            }.OrderBy(i => i.Name);
        }
    }
}
