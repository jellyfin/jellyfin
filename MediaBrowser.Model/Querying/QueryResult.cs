#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Querying
{
    public class QueryResult<T>
    {
        public QueryResult()
        {
            Items = Array.Empty<T>();
        }

        public QueryResult(IReadOnlyList<T> items)
        {
            Items = items;
            TotalRecordCount = items.Count;
        }

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
}
