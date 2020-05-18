#pragma warning disable CS1591

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Dlna
{
    public class SortCriteria
    {
        private string _value;

        public SortCriteria(string value)
        {
            _value = value;
        }

        public SortOrder SortOrder => SortOrder.Ascending;
    }
}
