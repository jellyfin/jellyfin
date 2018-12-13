using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Reflection;

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
        private readonly IAssemblyInfo _assemblyInfo;
        private readonly ITextLocalizer _textLocalizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationManager" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        public LocalizationManager(IServerConfigurationManager configurationManager, IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger, IAssemblyInfo assemblyInfo, ITextLocalizer textLocalizer)
        {
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
            _assemblyInfo = assemblyInfo;
            _textLocalizer = textLocalizer;

            ExtractAll();
        }

        private void ExtractAll()
        {
            var type = GetType();
            var resourcePath = type.Namespace + ".Ratings.";

            var localizationPath = LocalizationPath;

            _fileSystem.CreateDirectory(localizationPath);

            var existingFiles = GetRatingsFiles(localizationPath)
                .Select(Path.GetFileName)
                .ToList();

            // Extract from the assembly
            foreach (var resource in _assemblyInfo
                .GetManifestResourceNames(type)
                .Where(i => i.StartsWith(resourcePath)))
            {
                var filename = "ratings-" + resource.Substring(resourcePath.Length);

                if (!existingFiles.Contains(filename))
                {
                    using (var stream = _assemblyInfo.GetManifestResourceStream(type, resource))
                    {
                        var target = Path.Combine(localizationPath, filename);
                        _logger.LogInformation("Extracting ratings to {0}", target);

                        using (var fs = _fileSystem.GetFileStream(target, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
                        {
                            stream.CopyTo(fs);
                        }
                    }
                }
            }

            foreach (var file in GetRatingsFiles(localizationPath))
            {
                LoadRatings(file);
            }

            LoadAdditionalRatings();
        }

        private void LoadAdditionalRatings()
        {
            LoadRatings("au", new[] {

                new ParentalRating("AU-G", 1),
                new ParentalRating("AU-PG", 5),
                new ParentalRating("AU-M", 6),
                new ParentalRating("AU-MA15+", 7),
                new ParentalRating("AU-M15+", 8),
                new ParentalRating("AU-R18+", 9),
                new ParentalRating("AU-X18+", 10),
                new ParentalRating("AU-RC", 11)
            });

            LoadRatings("be", new[] {

                new ParentalRating("BE-AL", 1),
                new ParentalRating("BE-MG6", 2),
                new ParentalRating("BE-6", 3),
                new ParentalRating("BE-9", 5),
                new ParentalRating("BE-12", 6),
                new ParentalRating("BE-16", 8)
            });

            LoadRatings("de", new[] {

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

            LoadRatings("ru", new [] {

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

        private List<string> GetRatingsFiles(string directory)
        {
            return _fileSystem.GetFilePaths(directory, false)
                .Where(i => string.Equals(Path.GetExtension(i), ".txt", StringComparison.OrdinalIgnoreCase))
                .Where(i => Path.GetFileName(i).StartsWith("ratings-", StringComparison.OrdinalIgnoreCase))
                .ToList();
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

        public string RemoveDiacritics(string text)
        {
            return _textLocalizer.RemoveDiacritics(text);
        }

        public string NormalizeFormKD(string text)
        {
            return _textLocalizer.NormalizeFormKD(text);
        }

        private CultureDto[] _cultures;

        /// <summary>
        /// Gets the cultures.
        /// </summary>
        /// <returns>IEnumerable{CultureDto}.</returns>
        public CultureDto[] GetCultures()
        {
            var result = _cultures;
            if (result != null)
            {
                return result;
            }

            var type = GetType();
            var path = type.Namespace + ".iso6392.txt";

            var list = new List<CultureDto>();

            using (var stream = _assemblyInfo.GetManifestResourceStream(type, path))
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
                                var threeletterNames = new List<string> { parts[0] };
                                if (!string.IsNullOrWhiteSpace(parts[1]))
                                {
                                    threeletterNames.Add(parts[1]);
                                }

                                list.Add(new CultureDto
                                {
                                    DisplayName = parts[3],
                                    Name = parts[3],
                                    ThreeLetterISOLanguageNames = threeletterNames.ToArray(),
                                    TwoLetterISOLanguageName = parts[2]
                                });
                            }
                        }
                    }
                }
            }

            result = list.Where(i => !string.IsNullOrWhiteSpace(i.Name) &&
               !string.IsNullOrWhiteSpace(i.DisplayName) &&
               i.ThreeLetterISOLanguageNames.Length > 0 &&
               !string.IsNullOrWhiteSpace(i.TwoLetterISOLanguageName)).ToArray();

            _cultures = result;

            return result;
        }

        public CultureDto FindLanguageInfo(string language)
        {
            return GetCultures()
                .FirstOrDefault(i => string.Equals(i.DisplayName, language, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.Name, language, StringComparison.OrdinalIgnoreCase) ||
                i.ThreeLetterISOLanguageNames.Contains(language, StringComparer.OrdinalIgnoreCase) ||
                string.Equals(i.TwoLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>IEnumerable{CountryInfo}.</returns>
        public CountryInfo[] GetCountries()
        {
            // ToDo: DeserializeFromStream seems broken in this case
            string jsonCountries = "[{\"Name\":\"AF\",\"DisplayName\":\"Afghanistan\",\"TwoLetterISORegionName\":\"AF\",\"ThreeLetterISORegionName\":\"AFG\"},{\"Name\":\"AL\",\"DisplayName\":\"Albania\",\"TwoLetterISORegionName\":\"AL\",\"ThreeLetterISORegionName\":\"ALB\"},{\"Name\":\"DZ\",\"DisplayName\":\"Algeria\",\"TwoLetterISORegionName\":\"DZ\",\"ThreeLetterISORegionName\":\"DZA\"},{\"Name\":\"AR\",\"DisplayName\":\"Argentina\",\"TwoLetterISORegionName\":\"AR\",\"ThreeLetterISORegionName\":\"ARG\"},{\"Name\":\"AM\",\"DisplayName\":\"Armenia\",\"TwoLetterISORegionName\":\"AM\",\"ThreeLetterISORegionName\":\"ARM\"},{\"Name\":\"AU\",\"DisplayName\":\"Australia\",\"TwoLetterISORegionName\":\"AU\",\"ThreeLetterISORegionName\":\"AUS\"},{\"Name\":\"AT\",\"DisplayName\":\"Austria\",\"TwoLetterISORegionName\":\"AT\",\"ThreeLetterISORegionName\":\"AUT\"},{\"Name\":\"AZ\",\"DisplayName\":\"Azerbaijan\",\"TwoLetterISORegionName\":\"AZ\",\"ThreeLetterISORegionName\":\"AZE\"},{\"Name\":\"BH\",\"DisplayName\":\"Bahrain\",\"TwoLetterISORegionName\":\"BH\",\"ThreeLetterISORegionName\":\"BHR\"},{\"Name\":\"BD\",\"DisplayName\":\"Bangladesh\",\"TwoLetterISORegionName\":\"BD\",\"ThreeLetterISORegionName\":\"BGD\"},{\"Name\":\"BY\",\"DisplayName\":\"Belarus\",\"TwoLetterISORegionName\":\"BY\",\"ThreeLetterISORegionName\":\"BLR\"},{\"Name\":\"BE\",\"DisplayName\":\"Belgium\",\"TwoLetterISORegionName\":\"BE\",\"ThreeLetterISORegionName\":\"BEL\"},{\"Name\":\"BZ\",\"DisplayName\":\"Belize\",\"TwoLetterISORegionName\":\"BZ\",\"ThreeLetterISORegionName\":\"BLZ\"},{\"Name\":\"VE\",\"DisplayName\":\"Bolivarian Republic of Venezuela\",\"TwoLetterISORegionName\":\"VE\",\"ThreeLetterISORegionName\":\"VEN\"},{\"Name\":\"BO\",\"DisplayName\":\"Bolivia\",\"TwoLetterISORegionName\":\"BO\",\"ThreeLetterISORegionName\":\"BOL\"},{\"Name\":\"BA\",\"DisplayName\":\"Bosnia and Herzegovina\",\"TwoLetterISORegionName\":\"BA\",\"ThreeLetterISORegionName\":\"BIH\"},{\"Name\":\"BW\",\"DisplayName\":\"Botswana\",\"TwoLetterISORegionName\":\"BW\",\"ThreeLetterISORegionName\":\"BWA\"},{\"Name\":\"BR\",\"DisplayName\":\"Brazil\",\"TwoLetterISORegionName\":\"BR\",\"ThreeLetterISORegionName\":\"BRA\"},{\"Name\":\"BN\",\"DisplayName\":\"Brunei Darussalam\",\"TwoLetterISORegionName\":\"BN\",\"ThreeLetterISORegionName\":\"BRN\"},{\"Name\":\"BG\",\"DisplayName\":\"Bulgaria\",\"TwoLetterISORegionName\":\"BG\",\"ThreeLetterISORegionName\":\"BGR\"},{\"Name\":\"KH\",\"DisplayName\":\"Cambodia\",\"TwoLetterISORegionName\":\"KH\",\"ThreeLetterISORegionName\":\"KHM\"},{\"Name\":\"CM\",\"DisplayName\":\"Cameroon\",\"TwoLetterISORegionName\":\"CM\",\"ThreeLetterISORegionName\":\"CMR\"},{\"Name\":\"CA\",\"DisplayName\":\"Canada\",\"TwoLetterISORegionName\":\"CA\",\"ThreeLetterISORegionName\":\"CAN\"},{\"Name\":\"029\",\"DisplayName\":\"Caribbean\",\"TwoLetterISORegionName\":\"029\",\"ThreeLetterISORegionName\":\"029\"},{\"Name\":\"CL\",\"DisplayName\":\"Chile\",\"TwoLetterISORegionName\":\"CL\",\"ThreeLetterISORegionName\":\"CHL\"},{\"Name\":\"CO\",\"DisplayName\":\"Colombia\",\"TwoLetterISORegionName\":\"CO\",\"ThreeLetterISORegionName\":\"COL\"},{\"Name\":\"CD\",\"DisplayName\":\"Congo [DRC]\",\"TwoLetterISORegionName\":\"CD\",\"ThreeLetterISORegionName\":\"COD\"},{\"Name\":\"CR\",\"DisplayName\":\"Costa Rica\",\"TwoLetterISORegionName\":\"CR\",\"ThreeLetterISORegionName\":\"CRI\"},{\"Name\":\"HR\",\"DisplayName\":\"Croatia\",\"TwoLetterISORegionName\":\"HR\",\"ThreeLetterISORegionName\":\"HRV\"},{\"Name\":\"CZ\",\"DisplayName\":\"Czech Republic\",\"TwoLetterISORegionName\":\"CZ\",\"ThreeLetterISORegionName\":\"CZE\"},{\"Name\":\"DK\",\"DisplayName\":\"Denmark\",\"TwoLetterISORegionName\":\"DK\",\"ThreeLetterISORegionName\":\"DNK\"},{\"Name\":\"DO\",\"DisplayName\":\"Dominican Republic\",\"TwoLetterISORegionName\":\"DO\",\"ThreeLetterISORegionName\":\"DOM\"},{\"Name\":\"EC\",\"DisplayName\":\"Ecuador\",\"TwoLetterISORegionName\":\"EC\",\"ThreeLetterISORegionName\":\"ECU\"},{\"Name\":\"EG\",\"DisplayName\":\"Egypt\",\"TwoLetterISORegionName\":\"EG\",\"ThreeLetterISORegionName\":\"EGY\"},{\"Name\":\"SV\",\"DisplayName\":\"El Salvador\",\"TwoLetterISORegionName\":\"SV\",\"ThreeLetterISORegionName\":\"SLV\"},{\"Name\":\"ER\",\"DisplayName\":\"Eritrea\",\"TwoLetterISORegionName\":\"ER\",\"ThreeLetterISORegionName\":\"ERI\"},{\"Name\":\"EE\",\"DisplayName\":\"Estonia\",\"TwoLetterISORegionName\":\"EE\",\"ThreeLetterISORegionName\":\"EST\"},{\"Name\":\"ET\",\"DisplayName\":\"Ethiopia\",\"TwoLetterISORegionName\":\"ET\",\"ThreeLetterISORegionName\":\"ETH\"},{\"Name\":\"FO\",\"DisplayName\":\"Faroe Islands\",\"TwoLetterISORegionName\":\"FO\",\"ThreeLetterISORegionName\":\"FRO\"},{\"Name\":\"FI\",\"DisplayName\":\"Finland\",\"TwoLetterISORegionName\":\"FI\",\"ThreeLetterISORegionName\":\"FIN\"},{\"Name\":\"FR\",\"DisplayName\":\"France\",\"TwoLetterISORegionName\":\"FR\",\"ThreeLetterISORegionName\":\"FRA\"},{\"Name\":\"GE\",\"DisplayName\":\"Georgia\",\"TwoLetterISORegionName\":\"GE\",\"ThreeLetterISORegionName\":\"GEO\"},{\"Name\":\"DE\",\"DisplayName\":\"Germany\",\"TwoLetterISORegionName\":\"DE\",\"ThreeLetterISORegionName\":\"DEU\"},{\"Name\":\"GR\",\"DisplayName\":\"Greece\",\"TwoLetterISORegionName\":\"GR\",\"ThreeLetterISORegionName\":\"GRC\"},{\"Name\":\"GL\",\"DisplayName\":\"Greenland\",\"TwoLetterISORegionName\":\"GL\",\"ThreeLetterISORegionName\":\"GRL\"},{\"Name\":\"GT\",\"DisplayName\":\"Guatemala\",\"TwoLetterISORegionName\":\"GT\",\"ThreeLetterISORegionName\":\"GTM\"},{\"Name\":\"HT\",\"DisplayName\":\"Haiti\",\"TwoLetterISORegionName\":\"HT\",\"ThreeLetterISORegionName\":\"HTI\"},{\"Name\":\"HN\",\"DisplayName\":\"Honduras\",\"TwoLetterISORegionName\":\"HN\",\"ThreeLetterISORegionName\":\"HND\"},{\"Name\":\"HK\",\"DisplayName\":\"Hong Kong S.A.R.\",\"TwoLetterISORegionName\":\"HK\",\"ThreeLetterISORegionName\":\"HKG\"},{\"Name\":\"HU\",\"DisplayName\":\"Hungary\",\"TwoLetterISORegionName\":\"HU\",\"ThreeLetterISORegionName\":\"HUN\"},{\"Name\":\"IS\",\"DisplayName\":\"Iceland\",\"TwoLetterISORegionName\":\"IS\",\"ThreeLetterISORegionName\":\"ISL\"},{\"Name\":\"IN\",\"DisplayName\":\"India\",\"TwoLetterISORegionName\":\"IN\",\"ThreeLetterISORegionName\":\"IND\"},{\"Name\":\"ID\",\"DisplayName\":\"Indonesia\",\"TwoLetterISORegionName\":\"ID\",\"ThreeLetterISORegionName\":\"IDN\"},{\"Name\":\"IR\",\"DisplayName\":\"Iran\",\"TwoLetterISORegionName\":\"IR\",\"ThreeLetterISORegionName\":\"IRN\"},{\"Name\":\"IQ\",\"DisplayName\":\"Iraq\",\"TwoLetterISORegionName\":\"IQ\",\"ThreeLetterISORegionName\":\"IRQ\"},{\"Name\":\"IE\",\"DisplayName\":\"Ireland\",\"TwoLetterISORegionName\":\"IE\",\"ThreeLetterISORegionName\":\"IRL\"},{\"Name\":\"PK\",\"DisplayName\":\"Islamic Republic of Pakistan\",\"TwoLetterISORegionName\":\"PK\",\"ThreeLetterISORegionName\":\"PAK\"},{\"Name\":\"IL\",\"DisplayName\":\"Israel\",\"TwoLetterISORegionName\":\"IL\",\"ThreeLetterISORegionName\":\"ISR\"},{\"Name\":\"IT\",\"DisplayName\":\"Italy\",\"TwoLetterISORegionName\":\"IT\",\"ThreeLetterISORegionName\":\"ITA\"},{\"Name\":\"CI\",\"DisplayName\":\"Ivory Coast\",\"TwoLetterISORegionName\":\"CI\",\"ThreeLetterISORegionName\":\"CIV\"},{\"Name\":\"JM\",\"DisplayName\":\"Jamaica\",\"TwoLetterISORegionName\":\"JM\",\"ThreeLetterISORegionName\":\"JAM\"},{\"Name\":\"JP\",\"DisplayName\":\"Japan\",\"TwoLetterISORegionName\":\"JP\",\"ThreeLetterISORegionName\":\"JPN\"},{\"Name\":\"JO\",\"DisplayName\":\"Jordan\",\"TwoLetterISORegionName\":\"JO\",\"ThreeLetterISORegionName\":\"JOR\"},{\"Name\":\"KZ\",\"DisplayName\":\"Kazakhstan\",\"TwoLetterISORegionName\":\"KZ\",\"ThreeLetterISORegionName\":\"KAZ\"},{\"Name\":\"KE\",\"DisplayName\":\"Kenya\",\"TwoLetterISORegionName\":\"KE\",\"ThreeLetterISORegionName\":\"KEN\"},{\"Name\":\"KR\",\"DisplayName\":\"Korea\",\"TwoLetterISORegionName\":\"KR\",\"ThreeLetterISORegionName\":\"KOR\"},{\"Name\":\"KW\",\"DisplayName\":\"Kuwait\",\"TwoLetterISORegionName\":\"KW\",\"ThreeLetterISORegionName\":\"KWT\"},{\"Name\":\"KG\",\"DisplayName\":\"Kyrgyzstan\",\"TwoLetterISORegionName\":\"KG\",\"ThreeLetterISORegionName\":\"KGZ\"},{\"Name\":\"LA\",\"DisplayName\":\"Lao P.D.R.\",\"TwoLetterISORegionName\":\"LA\",\"ThreeLetterISORegionName\":\"LAO\"},{\"Name\":\"419\",\"DisplayName\":\"Latin America\",\"TwoLetterISORegionName\":\"419\",\"ThreeLetterISORegionName\":\"419\"},{\"Name\":\"LV\",\"DisplayName\":\"Latvia\",\"TwoLetterISORegionName\":\"LV\",\"ThreeLetterISORegionName\":\"LVA\"},{\"Name\":\"LB\",\"DisplayName\":\"Lebanon\",\"TwoLetterISORegionName\":\"LB\",\"ThreeLetterISORegionName\":\"LBN\"},{\"Name\":\"LY\",\"DisplayName\":\"Libya\",\"TwoLetterISORegionName\":\"LY\",\"ThreeLetterISORegionName\":\"LBY\"},{\"Name\":\"LI\",\"DisplayName\":\"Liechtenstein\",\"TwoLetterISORegionName\":\"LI\",\"ThreeLetterISORegionName\":\"LIE\"},{\"Name\":\"LT\",\"DisplayName\":\"Lithuania\",\"TwoLetterISORegionName\":\"LT\",\"ThreeLetterISORegionName\":\"LTU\"},{\"Name\":\"LU\",\"DisplayName\":\"Luxembourg\",\"TwoLetterISORegionName\":\"LU\",\"ThreeLetterISORegionName\":\"LUX\"},{\"Name\":\"MO\",\"DisplayName\":\"Macao S.A.R.\",\"TwoLetterISORegionName\":\"MO\",\"ThreeLetterISORegionName\":\"MAC\"},{\"Name\":\"MK\",\"DisplayName\":\"Macedonia (FYROM)\",\"TwoLetterISORegionName\":\"MK\",\"ThreeLetterISORegionName\":\"MKD\"},{\"Name\":\"MY\",\"DisplayName\":\"Malaysia\",\"TwoLetterISORegionName\":\"MY\",\"ThreeLetterISORegionName\":\"MYS\"},{\"Name\":\"MV\",\"DisplayName\":\"Maldives\",\"TwoLetterISORegionName\":\"MV\",\"ThreeLetterISORegionName\":\"MDV\"},{\"Name\":\"ML\",\"DisplayName\":\"Mali\",\"TwoLetterISORegionName\":\"ML\",\"ThreeLetterISORegionName\":\"MLI\"},{\"Name\":\"MT\",\"DisplayName\":\"Malta\",\"TwoLetterISORegionName\":\"MT\",\"ThreeLetterISORegionName\":\"MLT\"},{\"Name\":\"MX\",\"DisplayName\":\"Mexico\",\"TwoLetterISORegionName\":\"MX\",\"ThreeLetterISORegionName\":\"MEX\"},{\"Name\":\"MN\",\"DisplayName\":\"Mongolia\",\"TwoLetterISORegionName\":\"MN\",\"ThreeLetterISORegionName\":\"MNG\"},{\"Name\":\"ME\",\"DisplayName\":\"Montenegro\",\"TwoLetterISORegionName\":\"ME\",\"ThreeLetterISORegionName\":\"MNE\"},{\"Name\":\"MA\",\"DisplayName\":\"Morocco\",\"TwoLetterISORegionName\":\"MA\",\"ThreeLetterISORegionName\":\"MAR\"},{\"Name\":\"NP\",\"DisplayName\":\"Nepal\",\"TwoLetterISORegionName\":\"NP\",\"ThreeLetterISORegionName\":\"NPL\"},{\"Name\":\"NL\",\"DisplayName\":\"Netherlands\",\"TwoLetterISORegionName\":\"NL\",\"ThreeLetterISORegionName\":\"NLD\"},{\"Name\":\"NZ\",\"DisplayName\":\"New Zealand\",\"TwoLetterISORegionName\":\"NZ\",\"ThreeLetterISORegionName\":\"NZL\"},{\"Name\":\"NI\",\"DisplayName\":\"Nicaragua\",\"TwoLetterISORegionName\":\"NI\",\"ThreeLetterISORegionName\":\"NIC\"},{\"Name\":\"NG\",\"DisplayName\":\"Nigeria\",\"TwoLetterISORegionName\":\"NG\",\"ThreeLetterISORegionName\":\"NGA\"},{\"Name\":\"NO\",\"DisplayName\":\"Norway\",\"TwoLetterISORegionName\":\"NO\",\"ThreeLetterISORegionName\":\"NOR\"},{\"Name\":\"OM\",\"DisplayName\":\"Oman\",\"TwoLetterISORegionName\":\"OM\",\"ThreeLetterISORegionName\":\"OMN\"},{\"Name\":\"PA\",\"DisplayName\":\"Panama\",\"TwoLetterISORegionName\":\"PA\",\"ThreeLetterISORegionName\":\"PAN\"},{\"Name\":\"PY\",\"DisplayName\":\"Paraguay\",\"TwoLetterISORegionName\":\"PY\",\"ThreeLetterISORegionName\":\"PRY\"},{\"Name\":\"CN\",\"DisplayName\":\"People's Republic of China\",\"TwoLetterISORegionName\":\"CN\",\"ThreeLetterISORegionName\":\"CHN\"},{\"Name\":\"PE\",\"DisplayName\":\"Peru\",\"TwoLetterISORegionName\":\"PE\",\"ThreeLetterISORegionName\":\"PER\"},{\"Name\":\"PH\",\"DisplayName\":\"Philippines\",\"TwoLetterISORegionName\":\"PH\",\"ThreeLetterISORegionName\":\"PHL\"},{\"Name\":\"PL\",\"DisplayName\":\"Poland\",\"TwoLetterISORegionName\":\"PL\",\"ThreeLetterISORegionName\":\"POL\"},{\"Name\":\"PT\",\"DisplayName\":\"Portugal\",\"TwoLetterISORegionName\":\"PT\",\"ThreeLetterISORegionName\":\"PRT\"},{\"Name\":\"MC\",\"DisplayName\":\"Principality of Monaco\",\"TwoLetterISORegionName\":\"MC\",\"ThreeLetterISORegionName\":\"MCO\"},{\"Name\":\"PR\",\"DisplayName\":\"Puerto Rico\",\"TwoLetterISORegionName\":\"PR\",\"ThreeLetterISORegionName\":\"PRI\"},{\"Name\":\"QA\",\"DisplayName\":\"Qatar\",\"TwoLetterISORegionName\":\"QA\",\"ThreeLetterISORegionName\":\"QAT\"},{\"Name\":\"MD\",\"DisplayName\":\"Republica Moldova\",\"TwoLetterISORegionName\":\"MD\",\"ThreeLetterISORegionName\":\"MDA\"},{\"Name\":\"RE\",\"DisplayName\":\"RÃ©union\",\"TwoLetterISORegionName\":\"RE\",\"ThreeLetterISORegionName\":\"REU\"},{\"Name\":\"RO\",\"DisplayName\":\"Romania\",\"TwoLetterISORegionName\":\"RO\",\"ThreeLetterISORegionName\":\"ROU\"},{\"Name\":\"RU\",\"DisplayName\":\"Russia\",\"TwoLetterISORegionName\":\"RU\",\"ThreeLetterISORegionName\":\"RUS\"},{\"Name\":\"RW\",\"DisplayName\":\"Rwanda\",\"TwoLetterISORegionName\":\"RW\",\"ThreeLetterISORegionName\":\"RWA\"},{\"Name\":\"SA\",\"DisplayName\":\"Saudi Arabia\",\"TwoLetterISORegionName\":\"SA\",\"ThreeLetterISORegionName\":\"SAU\"},{\"Name\":\"SN\",\"DisplayName\":\"Senegal\",\"TwoLetterISORegionName\":\"SN\",\"ThreeLetterISORegionName\":\"SEN\"},{\"Name\":\"RS\",\"DisplayName\":\"Serbia\",\"TwoLetterISORegionName\":\"RS\",\"ThreeLetterISORegionName\":\"SRB\"},{\"Name\":\"CS\",\"DisplayName\":\"Serbia and Montenegro (Former)\",\"TwoLetterISORegionName\":\"CS\",\"ThreeLetterISORegionName\":\"SCG\"},{\"Name\":\"SG\",\"DisplayName\":\"Singapore\",\"TwoLetterISORegionName\":\"SG\",\"ThreeLetterISORegionName\":\"SGP\"},{\"Name\":\"SK\",\"DisplayName\":\"Slovakia\",\"TwoLetterISORegionName\":\"SK\",\"ThreeLetterISORegionName\":\"SVK\"},{\"Name\":\"SI\",\"DisplayName\":\"Slovenia\",\"TwoLetterISORegionName\":\"SI\",\"ThreeLetterISORegionName\":\"SVN\"},{\"Name\":\"SO\",\"DisplayName\":\"Soomaaliya\",\"TwoLetterISORegionName\":\"SO\",\"ThreeLetterISORegionName\":\"SOM\"},{\"Name\":\"ZA\",\"DisplayName\":\"South Africa\",\"TwoLetterISORegionName\":\"ZA\",\"ThreeLetterISORegionName\":\"ZAF\"},{\"Name\":\"ES\",\"DisplayName\":\"Spain\",\"TwoLetterISORegionName\":\"ES\",\"ThreeLetterISORegionName\":\"ESP\"},{\"Name\":\"LK\",\"DisplayName\":\"Sri Lanka\",\"TwoLetterISORegionName\":\"LK\",\"ThreeLetterISORegionName\":\"LKA\"},{\"Name\":\"SE\",\"DisplayName\":\"Sweden\",\"TwoLetterISORegionName\":\"SE\",\"ThreeLetterISORegionName\":\"SWE\"},{\"Name\":\"CH\",\"DisplayName\":\"Switzerland\",\"TwoLetterISORegionName\":\"CH\",\"ThreeLetterISORegionName\":\"CHE\"},{\"Name\":\"SY\",\"DisplayName\":\"Syria\",\"TwoLetterISORegionName\":\"SY\",\"ThreeLetterISORegionName\":\"SYR\"},{\"Name\":\"TW\",\"DisplayName\":\"Taiwan\",\"TwoLetterISORegionName\":\"TW\",\"ThreeLetterISORegionName\":\"TWN\"},{\"Name\":\"TJ\",\"DisplayName\":\"Tajikistan\",\"TwoLetterISORegionName\":\"TJ\",\"ThreeLetterISORegionName\":\"TAJ\"},{\"Name\":\"TH\",\"DisplayName\":\"Thailand\",\"TwoLetterISORegionName\":\"TH\",\"ThreeLetterISORegionName\":\"THA\"},{\"Name\":\"TT\",\"DisplayName\":\"Trinidad and Tobago\",\"TwoLetterISORegionName\":\"TT\",\"ThreeLetterISORegionName\":\"TTO\"},{\"Name\":\"TN\",\"DisplayName\":\"Tunisia\",\"TwoLetterISORegionName\":\"TN\",\"ThreeLetterISORegionName\":\"TUN\"},{\"Name\":\"TR\",\"DisplayName\":\"Turkey\",\"TwoLetterISORegionName\":\"TR\",\"ThreeLetterISORegionName\":\"TUR\"},{\"Name\":\"TM\",\"DisplayName\":\"Turkmenistan\",\"TwoLetterISORegionName\":\"TM\",\"ThreeLetterISORegionName\":\"TKM\"},{\"Name\":\"AE\",\"DisplayName\":\"U.A.E.\",\"TwoLetterISORegionName\":\"AE\",\"ThreeLetterISORegionName\":\"ARE\"},{\"Name\":\"UA\",\"DisplayName\":\"Ukraine\",\"TwoLetterISORegionName\":\"UA\",\"ThreeLetterISORegionName\":\"UKR\"},{\"Name\":\"GB\",\"DisplayName\":\"United Kingdom\",\"TwoLetterISORegionName\":\"GB\",\"ThreeLetterISORegionName\":\"GBR\"},{\"Name\":\"US\",\"DisplayName\":\"United States\",\"TwoLetterISORegionName\":\"US\",\"ThreeLetterISORegionName\":\"USA\"},{\"Name\":\"UY\",\"DisplayName\":\"Uruguay\",\"TwoLetterISORegionName\":\"UY\",\"ThreeLetterISORegionName\":\"URY\"},{\"Name\":\"UZ\",\"DisplayName\":\"Uzbekistan\",\"TwoLetterISORegionName\":\"UZ\",\"ThreeLetterISORegionName\":\"UZB\"},{\"Name\":\"VN\",\"DisplayName\":\"Vietnam\",\"TwoLetterISORegionName\":\"VN\",\"ThreeLetterISORegionName\":\"VNM\"},{\"Name\":\"YE\",\"DisplayName\":\"Yemen\",\"TwoLetterISORegionName\":\"YE\",\"ThreeLetterISORegionName\":\"YEM\"},{\"Name\":\"ZW\",\"DisplayName\":\"Zimbabwe\",\"TwoLetterISORegionName\":\"ZW\",\"ThreeLetterISORegionName\":\"ZWE\"}]";

            return _jsonSerializer.DeserializeFromString<CountryInfo[]>(jsonCountries);
        }

        /// <summary>
        /// Gets the parental ratings.
        /// </summary>
        /// <returns>IEnumerable{ParentalRating}.</returns>
        public ParentalRating[] GetParentalRatings()
        {
            return GetParentalRatingsDictionary().Values.ToArray();
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
            var dict = _fileSystem.ReadAllLines(file).Select(i =>
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

            _allParentalRatings[countryCode] = dict;
        }

        private readonly string[] _unratedValues = { "n/a", "unrated", "not rated" };

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

            if (ratingsDictionary.TryGetValue(rating, out value))
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

            string value;

            if (dictionary.TryGetValue(phrase, out value))
            {
                return value;
            }

            return phrase;
        }

        const string DefaultCulture = "en-US";

        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _dictionaries =
            new ConcurrentDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> GetLocalizationDictionary(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException("culture");
            }

            const string prefix = "Core";
            var key = prefix + culture;

            return _dictionaries.GetOrAdd(key, k => GetDictionary(prefix, culture, DefaultCulture + ".json"));
        }

        private Dictionary<string, string> GetDictionary(string prefix, string culture, string baseFilename)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException("culture");
            }

            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var namespaceName = GetType().Namespace + "." + prefix;

            CopyInto(dictionary, namespaceName + "." + baseFilename);
            CopyInto(dictionary, namespaceName + "." + GetResourceFilename(culture));

            return dictionary;
        }

        private void CopyInto(IDictionary<string, string> dictionary, string resourcePath)
        {
            using (var stream = _assemblyInfo.GetManifestResourceStream(GetType(), resourcePath))
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

        public LocalizatonOption[] GetLocalizationOptions()
        {
            return new LocalizatonOption[]
            {
                new LocalizatonOption{ Name="Arabic", Value="ar"},
                new LocalizatonOption{ Name="Belarusian (Belarus)", Value="be-BY"},
                new LocalizatonOption{ Name="Bulgarian (Bulgaria)", Value="bg-BG"},
                new LocalizatonOption{ Name="Catalan", Value="ca"},
                new LocalizatonOption{ Name="Chinese Simplified", Value="zh-CN"},
                new LocalizatonOption{ Name="Chinese Traditional", Value="zh-TW"},
                new LocalizatonOption{ Name="Chinese Traditional (Hong Kong)", Value="zh-HK"},
                new LocalizatonOption{ Name="Croatian", Value="hr"},
                new LocalizatonOption{ Name="Czech", Value="cs"},
                new LocalizatonOption{ Name="Danish", Value="da"},
                new LocalizatonOption{ Name="Dutch", Value="nl"},
                new LocalizatonOption{ Name="English (United Kingdom)", Value="en-GB"},
                new LocalizatonOption{ Name="English (United States)", Value="en-US"},
                new LocalizatonOption{ Name="Finnish", Value="fi"},
                new LocalizatonOption{ Name="French", Value="fr"},
                new LocalizatonOption{ Name="French (Canada)", Value="fr-CA"},
                new LocalizatonOption{ Name="German", Value="de"},
                new LocalizatonOption{ Name="Greek", Value="el"},
                new LocalizatonOption{ Name="Hebrew", Value="he"},
                new LocalizatonOption{ Name="Hindi (India)", Value="hi-IN"},
                new LocalizatonOption{ Name="Hungarian", Value="hu"},
                new LocalizatonOption{ Name="Indonesian", Value="id"},
                new LocalizatonOption{ Name="Italian", Value="it"},
                new LocalizatonOption{ Name="Japanese", Value="ja"},
                new LocalizatonOption{ Name="Kazakh", Value="kk"},
                new LocalizatonOption{ Name="Korean", Value="ko"},
                new LocalizatonOption{ Name="Lithuanian", Value="lt-LT"},
                new LocalizatonOption{ Name="Malay", Value="ms"},
                new LocalizatonOption{ Name="Norwegian Bokmål", Value="nb"},
                new LocalizatonOption{ Name="Persian", Value="fa"},
                new LocalizatonOption{ Name="Polish", Value="pl"},
                new LocalizatonOption{ Name="Portuguese (Brazil)", Value="pt-BR"},
                new LocalizatonOption{ Name="Portuguese (Portugal)", Value="pt-PT"},
                new LocalizatonOption{ Name="Romanian", Value="ro"},
                new LocalizatonOption{ Name="Russian", Value="ru"},
                new LocalizatonOption{ Name="Slovak", Value="sk"},
                new LocalizatonOption{ Name="Slovenian (Slovenia)", Value="sl-SI"},
                new LocalizatonOption{ Name="Spanish", Value="es"},
                new LocalizatonOption{ Name="Spanish (Latin America)", Value="es-419"},
                new LocalizatonOption{ Name="Spanish (Mexico)", Value="es-MX"},
                new LocalizatonOption{ Name="Swedish", Value="sv"},
                new LocalizatonOption{ Name="Swiss German", Value="gsw"},
                new LocalizatonOption{ Name="Turkish", Value="tr"},
                new LocalizatonOption{ Name="Ukrainian", Value="uk"},
                new LocalizatonOption{ Name="Vietnamese", Value="vi"}

            };
        }
    }

    public interface ITextLocalizer
    {
        string RemoveDiacritics(string text);

        string NormalizeFormKD(string text);
    }
}
