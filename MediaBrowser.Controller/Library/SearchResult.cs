using System;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Represents an item matched by a search query with its relevance score.
/// </summary>
public readonly struct SearchResult : IEquatable<SearchResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchResult"/> struct.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="score">The relevance score.</param>
    public SearchResult(Guid itemId, float score)
    {
        ItemId = itemId;
        Score = score;
    }

    /// <summary>
    /// Gets the ID of the matching item.
    /// </summary>
    public Guid ItemId { get; init; }

    /// <summary>
    /// Gets the relevance score. Higher values indicate more relevant results.
    /// </summary>
    public float Score { get; init; }

    /// <summary>
    /// Compares two <see cref="SearchResult"/> instances for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if the instances are equal; otherwise, false.</returns>
    public static bool operator ==(SearchResult left, SearchResult right)
        => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="SearchResult"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if the instances are not equal; otherwise, false.</returns>
    public static bool operator !=(SearchResult left, SearchResult right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is SearchResult other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(SearchResult other)
        => ItemId.Equals(other.ItemId) && Score.Equals(other.Score);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(ItemId, Score);
}
