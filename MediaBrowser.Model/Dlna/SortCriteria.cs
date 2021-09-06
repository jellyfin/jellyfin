#pragma warning disable CS1591

using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Dlna
{
    public class SortCriteria
    {
        public SortCriteria(string value)
        {
        }

        public SortOrder SortOrder => SortOrder.Ascending;
    }
}
