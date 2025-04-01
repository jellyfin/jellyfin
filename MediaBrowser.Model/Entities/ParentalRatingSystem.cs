using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Entities;

/// <summary>
/// A class representing a parental rating system.
/// </summary>
public class ParentalRatingSystem
{
    /// <summary>
    /// Gets or sets the country code.
    /// </summary>
    [JsonPropertyName("countryCode")]
    public required string CountryCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sub scores are supported.
    /// </summary>
    [JsonPropertyName("supportsSubScores")]
    public bool SupportsSubScores { get; set; }

    /// <summary>
    /// Gets or sets the ratings.
    /// </summary>
    [JsonPropertyName("ratings")]
    public IReadOnlyList<ParentalRatingEntry>? Ratings { get; set; }
}
