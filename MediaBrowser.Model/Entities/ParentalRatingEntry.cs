using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Entities;

/// <summary>
/// A class representing an parental rating entry.
/// </summary>
public class ParentalRatingEntry
{
    /// <summary>
    /// Gets or sets the rating strings.
    /// </summary>
    [JsonPropertyName("ratingStrings")]
    public required IReadOnlyList<string> RatingStrings { get; set; }

    /// <summary>
    /// Gets or sets the score.
    /// </summary>
    [JsonPropertyName("ratingScore")]
    public required ParentalRatingScore RatingScore { get; set; }
}
