using System;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// An item's id paired with whether a given user has played it.
/// </summary>
/// <param name="Id">The id of the item.</param>
/// <param name="Played">Whether the user has played the item.</param>
public readonly record struct LeafPlayedState(Guid Id, bool Played);
