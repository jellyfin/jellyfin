using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Interface ILocalizationManager
    /// </summary>
    public interface ILocalizationManager
    {
        /// <summary>
        /// Gets the cultures.
        /// </summary>
        /// <returns>IEnumerable{CultureDto}.</returns>
        IEnumerable<CultureDto> GetCultures();
        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>IEnumerable{CountryInfo}.</returns>
        IEnumerable<CountryInfo> GetCountries();
        /// <summary>
        /// Gets the parental ratings.
        /// </summary>
        /// <returns>IEnumerable{ParentalRating}.</returns>
        IEnumerable<ParentalRating> GetParentalRatings();
    }
}
