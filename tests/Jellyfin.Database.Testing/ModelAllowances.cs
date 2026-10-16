using System.Collections.Generic;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Where the model of an engine may differ from the SQLite model, each by exactly what it names.
/// </summary>
/// <param name="UnexpressibleIndexFilters">The exact filters of indexes that are not unique, which the engine has over every row.</param>
/// <param name="NoActionCascades">The foreign keys, by table and columns, that cascade in the SQLite model and delete nothing on the engine.</param>
/// <param name="NullFilteredUniqueIndexes">
/// The unique indexes, by table and columns, that the engine filters to the rows where the columns have a value, with the
/// exact filter.
/// </param>
public sealed record ModelAllowances(
    IReadOnlyCollection<string> UnexpressibleIndexFilters,
    IReadOnlyCollection<(string Table, string Columns)> NoActionCascades,
    IReadOnlyCollection<(string Table, string Columns, string Filter)> NullFilteredUniqueIndexes)
{
    /// <summary>
    /// Gets the allowances of an engine whose model has the shape of the SQLite model.
    /// </summary>
    public static ModelAllowances None { get; } = new([], [], []);
}
