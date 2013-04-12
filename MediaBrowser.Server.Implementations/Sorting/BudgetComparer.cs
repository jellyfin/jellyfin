using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Server.Implementations.Sorting
{
    public class BudgetDateComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return (x.Budget ?? 0).CompareTo(y.Budget ?? 0);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.Budget; }
        }
    }
}
