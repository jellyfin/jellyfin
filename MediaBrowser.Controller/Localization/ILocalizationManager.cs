using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using System;
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
        /// Localizes the document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="tokenBuilder">The token builder.</param>
        /// <returns>System.String.</returns>
        string LocalizeDocument(string document, string culture, Func<string, string> tokenBuilder);

        /// <summary>
        /// Gets the localization options.
        /// </summary>
        /// <returns>IEnumerable{LocalizatonOption}.</returns>
        IEnumerable<LocalizatonOption> GetLocalizationOptions();
    }
}
