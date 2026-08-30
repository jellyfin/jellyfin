using System.Collections.Generic;
using Jellyfin.Api.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Models;

/// <summary>
/// Query options for filtering and ordering items by their pre-transliteration sort-name initial.
/// </summary>
public class NameInitialQuery
{
    /// <summary>
    /// Gets or sets the initials to include.
    /// </summary>
    [FromQuery(Name = "nameInitials")]
    [ModelBinder(typeof(CommaDelimitedCollectionModelBinder))]
    public IReadOnlyList<string> NameInitials { get; set; } = [];

    /// <summary>
    /// Gets or sets the initials to exclude.
    /// </summary>
    [FromQuery(Name = "excludeNameInitials")]
    [ModelBinder(typeof(CommaDelimitedCollectionModelBinder))]
    public IReadOnlyList<string> ExcludeNameInitials { get; set; } = [];

    /// <summary>
    /// Gets or sets the ordered initials used to group SortName or Name ordering.
    /// </summary>
    [FromQuery(Name = "nameInitialSortOrder")]
    [ModelBinder(typeof(CommaDelimitedCollectionModelBinder))]
    public IReadOnlyList<string> NameInitialSortOrder { get; set; } = [];
}
