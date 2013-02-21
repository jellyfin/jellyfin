using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Since it can be slow to collect this data, this class helps provide a way to calculate them all at once.
    /// </summary>
    [ProtoContract]
    public class ItemSpecialCounts
    {
        [ProtoMember(1)]
        public int RecentlyAddedItemCount { get; set; }

        [ProtoMember(2)]
        public int RecentlyAddedUnPlayedItemCount { get; set; }

        [ProtoMember(3)]
        public int InProgressItemCount { get; set; }

        [ProtoMember(4)]
        public decimal PlayedPercentage { get; set; }
    }
}
