using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationManager"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public LocalizationManager(IServerConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;

            ExtractAll();
        }

        private void ExtractAll()
        {
            var type = GetType();
            var resourcePath = type.Namespace + ".Ratings.";

            var localizationPath = LocalizationPath;

            Directory.CreateDirectory(localizationPath);

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
                        using (var fs = new FileStream(Path.Combine(localizationPath, filename), FileMode.Create, FileAccess.Write, FileShare.Read))
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
            return CultureInfo.GetCultures(CultureTypes.AllCultures)
                .OrderBy(c => c.DisplayName)
                .DistinctBy(c => c.TwoLetterISOLanguageName + c.ThreeLetterISOLanguageName)
                .Select(c => new CultureDto
                {
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    ThreeLetterISOLanguageName = c.ThreeLetterISOLanguageName,
                    TwoLetterISOLanguageName = c.TwoLetterISOLanguageName
                });
        }

        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>IEnumerable{CountryInfo}.</returns>
        public IEnumerable<CountryInfo> GetCountries()
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(c => new RegionInfo(c.LCID))
                .OrderBy(c => c.DisplayName)
                .DistinctBy(c => c.TwoLetterISORegionName)
                .Select(c => new CountryInfo
                {
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    TwoLetterISORegionName = c.TwoLetterISORegionName,
                    ThreeLetterISORegionName = c.ThreeLetterISORegionName
                });
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

            var countryCode = Path.GetFileNameWithoutExtension(file).Split('-').Last();

            _allParentalRatings.TryAdd(countryCode, dict);
        }

        /// <summary>
        /// Gets the rating level.
        /// </summary>
        public int? GetRatingLevel(string rating)
        {
            if (string.IsNullOrEmpty(rating))
            {
                throw new ArgumentNullException("rating");
            }

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
    }
}
