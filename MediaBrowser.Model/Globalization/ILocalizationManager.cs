using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Globalization;

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
    /// <returns><see cref="IReadOnlyList{CountryInfo}" />.</returns>
    IReadOnlyList<CountryInfo> GetCountries();

    /// <summary>
    /// Gets the parental ratings.
    /// </summary>
    /// <returns><see cref="IReadOnlyList{ParentalRating}" />.</returns>
    IReadOnlyList<ParentalRating> GetParentalRatings();

    /// <summary>
    /// Gets the rating level.
    /// </summary>
    /// <param name="rating">The rating.</param>
    /// <param name="countryCode">The optional two letter ISO language string.</param>
    /// <returns><see cref="ParentalRatingScore" /> or <c>null</c>.</returns>
    ParentalRatingScore? GetRatingScore(string rating, string? countryCode = null);

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
    /// <returns><see cref="IEnumerable{LocalizationOption}" />.</returns>
    IEnumerable<LocalizationOption> GetLocalizationOptions();

    /// <summary>
    /// Returns the correct <see cref="CultureDto" /> for the given language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The correct <see cref="CultureDto" /> for the given language.</returns>
    CultureDto? FindLanguageInfo(string language);

    /// <summary>
    /// Returns the language in ISO 639-2/T when the input is ISO 639-2/B.
    /// </summary>
    /// <param name="isoB">The language in ISO 639-2/B.</param>
    /// <param name="isoT">The language in ISO 639-2/T.</param>
    /// <returns>Whether the language could be converted.</returns>
    public bool TryGetISO6392TFromB(string isoB, [NotNullWhen(true)] out string? isoT);
}
