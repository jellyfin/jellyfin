using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// What renaming a genre or studio item carried onto the items naming it.
/// </summary>
/// <param name="PreviousName">The name those items carried before, or <c>null</c> when nothing was renamed.</param>
/// <param name="ItemIds">The items whose stored name was rewritten.</param>
public sealed record ByNameRename(
    string? PreviousName,
    IReadOnlyList<Guid> ItemIds)
{
    /// <summary>
    /// Gets the result of a rename that changed nothing.
    /// </summary>
    public static ByNameRename None { get; } = new(null, Array.Empty<Guid>());
}
