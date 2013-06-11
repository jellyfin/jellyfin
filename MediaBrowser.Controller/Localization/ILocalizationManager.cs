using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// <summary>
        /// Gets the rating level.
        /// </summary>
        /// <param name="rating">The rating.</param>
        /// <returns>System.Int32.</returns>
        int? GetRatingLevel(string rating);
    }
}
