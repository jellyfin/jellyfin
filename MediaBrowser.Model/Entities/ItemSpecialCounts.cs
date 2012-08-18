
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Since it can be slow to collect this data, this class helps provide a way to calculate them all at once.
    /// </summary>
    public class ItemSpecialCounts
    {
        public int RecentlyAddedItemCount { get; set; }
        public int RecentlyAddedUnPlayedItemCount { get; set; }
        public int InProgressItemCount { get; set; }
        public decimal WatchedPercentage { get; set; }
    }
}
