#pragma warning disable CS1591

using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Dlna
{
    public class SortCriteria
    {
        public SortOrder SortOrder => SortOrder.Ascending;

        public SortCriteria(string value)
        {
        }
    }
}
