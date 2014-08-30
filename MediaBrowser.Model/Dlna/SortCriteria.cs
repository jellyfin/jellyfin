using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Dlna
{
    public class SortCriteria
    {
        public SortOrder SortOrder
        {
            get { return SortOrder.Ascending; }
        }

        public SortCriteria(string value)
        {
            
        }
    }
}
