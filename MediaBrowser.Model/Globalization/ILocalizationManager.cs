#nullable disable
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Globalization
{
    /// <summary>
    /// Interface ILocalizationManager.
    /// </summary>
    public interface ILocalizationManager
    {
        /// <summary>
        /// Gets the cultures.
        /// </summary>
        /// <returns><see cref="IEnumerable{CultureDto}" />.</returns>
        IEnumerable<CultureDto> GetCultures();

        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns><see cref="IEnumerable{CountryInfo}" />.</returns>
        IEnumerable<CountryInfo> GetCountries();

        /// <summary>
        /// Gets the parental ratings.
        /// </summary>
        /// <returns><see cref="IEnumerable{ParentalRating}" />.</returns>
        IEnumerable<ParentalRating> GetParentalRatings();

        /// <summary>
        /// Gets the rating level.
        /// </summary>
        /// <param name="rating">The rating.</param>
        /// <returns><see cref="int" /> or <c>null</c>.</returns>
        int? GetRatingLevel(string rating);

        /// <summary>
        /// Gets the localized string.
        /// </summary>
        /// <param name="phrase">The phrase.</param>
        /// <param name="culture">The culture.</param>
        /// <returns><see cref="string" />.</returns>
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
        /// <returns><see cref="IEnumerable{LocalizatonOption}" />.</returns>
        IEnumerable<LocalizationOption> GetLocalizationOptions();

        /// <summary>
        /// Checks if the string contains a character with the specified unicode category.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <param name="category">The unicode category.</param>
        /// <returns>Wether or not the string contains a character with the specified unicode category.</returns>
        bool HasUnicodeCategory(string value, UnicodeCategory category);

        /// <summary>
        /// Returns the correct <see cref="CultureInfo" /> for the given language.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>The correct <see cref="CultureInfo" /> for the given language.</returns>
        CultureDto FindLanguageInfo(string language);
    }
}
