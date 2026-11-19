namespace Emby.Server.Implementations.Localization;

/// <summary>
/// The persisted form of <see cref="RatingsState"/>: what the stored parental rating scores were last computed for.
/// </summary>
/// <remarks>
/// Public only because it is stored as a named configuration (XML serialization). It is internal bookkeeping,
/// not an API model, and is excluded from the OpenAPI specification.
/// </remarks>
public class RatingsStateData
{
    /// <summary>
    /// Gets or sets the ratings data version of the last completed recompute.
    /// </summary>
    public string RatingsDataVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metadata country fingerprint of the last completed recompute.
    /// </summary>
    public string CountryFingerprint { get; set; } = string.Empty;
}
