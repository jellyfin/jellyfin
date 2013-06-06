using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MoreLinq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Localization
{
    /// <summary>
    /// Class LocalizationManager
    /// </summary>
    public class LocalizationManager : ILocalizationManager
    {
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
            return Ratings.RatingsDict
                .Select(k => new ParentalRating {Name = k.Key, Value = k.Value})
                .OrderBy(p => p.Value)
                .Where(p => p.Value > 0);
        }
    }
}
