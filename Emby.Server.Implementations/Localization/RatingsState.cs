namespace Emby.Server.Implementations.Localization;

/// <summary>
/// What the stored parental rating scores depend on, besides the items themselves.
/// </summary>
/// <param name="RatingsDataVersion">The version of the rating data, see <c>LocalizationManager.RatingsDataVersion</c>.</param>
/// <param name="CountryFingerprint">A fingerprint of the server and explicit library metadata countries.</param>
public readonly record struct RatingsState(string RatingsDataVersion, string CountryFingerprint);
