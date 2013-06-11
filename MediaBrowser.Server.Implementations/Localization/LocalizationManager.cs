using MediaBrowser.Common.IO;
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
using System.Threading.Tasks;

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

            if (!_allParentalRatings.TryGetValue(countryCode, out value))
            {
                value = LoadRatings(countryCode);

                if (value != null)
                {
                    _allParentalRatings.TryAdd(countryCode, value);
                }
            }

            return value;
        }

        /// <summary>
        /// Loads the ratings.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        private Dictionary<string, ParentalRating> LoadRatings(string countryCode)
        {
            var path = GetRatingsFilePath(countryCode);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return File.ReadAllLines(path).Select(i =>
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
            .ToDictionary(i => i.Name);
        }

        /// <summary>
        /// Gets the ratings file.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        private string GetRatingsFilePath(string countryCode)
        {
            countryCode = countryCode.ToLower();

            var path = Path.Combine(LocalizationPath, "ratings-" + countryCode + ".txt");

            if (!File.Exists(path))
            {
                // Extract embedded resource

                var type = GetType();
                var resourcePath = type.Namespace + ".Ratings." + countryCode + ".txt";

                using (var stream = type.Assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    var parentPath = Path.GetDirectoryName(path);

                    if (!Directory.Exists(parentPath))
                    {
                        Directory.CreateDirectory(parentPath);
                    }

                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }

            return path;
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
                var stripped = StripCountry(rating);

                ratingsDictionary.TryGetValue(stripped, out value);
            }

            return value == null ? (int?)null : value.Value;
        }

        /// <summary>
        /// Strips the country.
        /// </summary>
        /// <param name="rating">The rating.</param>
        /// <returns>System.String.</returns>
        private static string StripCountry(string rating)
        {
            int start = rating.IndexOf('-');
            return start > 0 ? rating.Substring(start + 1) : rating;
        }
    }
}
