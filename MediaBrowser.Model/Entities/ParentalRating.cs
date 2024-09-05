#nullable disable

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class ParentalRating.
/// </summary>
public class ParentalRating
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParentalRating"/> class.
    /// </summary>
    public ParentalRating()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParentalRating"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    public ParentalRating(string name, double? value)
    {
        Name = name;
        Value = value;
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
    public double? Value { get; set; }
}
