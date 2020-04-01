#pragma warning disable CS1591

using MediaBrowser.Model.Entities;

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
