using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using System.Globalization;

namespace MediaBrowser.Model.Globalization
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
        CultureDto[] GetCultures();
        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>IEnumerable{CountryInfo}.</returns>
        CountryInfo[] GetCountries();
        /// <summary>
        /// Gets the parental ratings.
        /// </summary>
        /// <returns>IEnumerable{ParentalRating}.</returns>
        ParentalRating[] GetParentalRatings();
        /// <summary>
        /// Gets the rating level.
        /// </summary>
        /// <param name="rating">The rating.</param>
        /// <returns>System.Int32.</returns>
        int? GetRatingLevel(string rating);

        /// <summary>
        /// Gets the localized string.
        /// </summary>
        /// <param name="phrase">The phrase.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>System.String.</returns>
        string GetLocalizedString(string phrase, string culture);

        /// <summary>
        /// Gets the localized string.
        /// </summary>
        /// <param name="phrase">The phrase.</param>
        /// <returns>System.String.</returns>
        string GetLocalizedString(string phrase);

        /// <summary>
        /// Gets the localization options.
        /// </summary>
        /// <returns>IEnumerable{LocalizatonOption}.</returns>
        LocalizatonOption[] GetLocalizationOptions();

        string RemoveDiacritics(string text);

        string NormalizeFormKD(string text);

        bool HasUnicodeCategory(string value, UnicodeCategory category);

        CultureDto FindLanguageInfo(string language);
    }
}
