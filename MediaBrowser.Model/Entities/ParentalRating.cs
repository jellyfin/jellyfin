namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class ParentalRating.
/// </summary>
public class ParentalRating
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParentalRating"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="score">The score.</param>
    public ParentalRating(string name, ParentalRatingScore? score)
    {
        Name = name;
        Value = score?.Score;
        RatingScore = score;
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>The value.</value>
    /// <remarks>
    /// Deprecated.
    /// </remarks>
    public int? Value { get; set; }

    /// <summary>
    /// Gets or sets the rating score.
    /// </summary>
    /// <value>The rating score.</value>
    public ParentalRatingScore? RatingScore { get; set; }
}
