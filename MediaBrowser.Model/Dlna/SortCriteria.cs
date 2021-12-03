#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Dlna
{
    public class SortCriteria
    {
        public SortCriteria(string sortOrder)
        {
            if (!string.IsNullOrEmpty(sortOrder) && Enum.TryParse<SortOrder>(sortOrder, true, out var sortOrderValue))
            {
                SortOrder = sortOrderValue;
            }
            else
            {
                SortOrder = SortOrder.Ascending;
            }
        }

        public SortOrder SortOrder { get; }
    }
}
