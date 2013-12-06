using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Server.Implementations.Sorting
{
    class VideoBitRateComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetValue(x).CompareTo(GetValue(y));
        }

        private int GetValue(BaseItem item)
        {
            var video = item as Video;

            if (video != null)
            {
                return video.VideoBitRate ?? 0;
            }

            return 0;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.VideoBitRate; }
        }
    }
}
