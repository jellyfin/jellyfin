using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Querying;

/// <summary>
/// Query result container.
/// </summary>
/// <typeparam name="T">The type of item contained in the query result.</typeparam>
public class QueryResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryResult{T}" /> class.
    /// </summary>
    public QueryResult()
    {
        Items = Array.Empty<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryResult{T}" /> class.
    /// </summary>
    /// <param name="items">The list of items.</param>
    public QueryResult(IReadOnlyList<T> items)
    {
        Items = items;
        TotalRecordCount = items.Count;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryResult{T}" /> class.
    /// </summary>
    /// <param name="startIndex">The start index that was used to build the item list.</param>
    /// <param name="totalRecordCount">The total count of items.</param>
    /// <param name="items">The list of items.</param>
    public QueryResult(int? startIndex, int? totalRecordCount, IReadOnlyList<T> items)
    {
        StartIndex = startIndex ?? 0;
        TotalRecordCount = totalRecordCount ?? items.Count;
        Items = items;
    }

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    /// <value>The items.</value>
    public IReadOnlyList<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the total number of records available.
    /// </summary>
    /// <value>The total record count.</value>
    public int TotalRecordCount { get; set; }

    /// <summary>
    /// Gets or sets the index of the first record in Items.
    /// </summary>
    /// <value>First record index.</value>
    public int StartIndex { get; set; }
}
