using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Entities;

/// <summary>
/// A class representing an parental rating score.
/// </summary>
public class ParentalRatingScore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParentalRatingScore"/> class.
    /// </summary>
    /// <param name="score">The score.</param>
    /// <param name="subScore">The sub score.</param>
    public ParentalRatingScore(int score, int? subScore)
    {
        Score = score;
        SubScore = subScore;
    }

    /// <summary>
    /// Gets or sets the score.
    /// </summary>
    [JsonPropertyName("score")]
    public int Score { get; set; }

    /// <summary>
    /// Gets or sets the sub score.
    /// </summary>
    [JsonPropertyName("subScore")]
    public int? SubScore { get; set; }
}
