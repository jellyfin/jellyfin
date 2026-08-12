using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// The genre and studio items one item links to.
/// </summary>
/// <param name="Genres">The genre items, by id and the name each carries now.</param>
/// <param name="Studios">The studio items, by id and the name each carries now.</param>
public sealed record ItemByNameLinks(
    IReadOnlyList<NameGuidPair> Genres,
    IReadOnlyList<NameGuidPair> Studios)
{
    /// <summary>
    /// Gets an item with no links.
    /// </summary>
    public static ItemByNameLinks Empty { get; } = new([], []);
}
