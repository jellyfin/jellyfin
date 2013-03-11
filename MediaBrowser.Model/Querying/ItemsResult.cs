using MediaBrowser.Model.Dto;
using ProtoBuf;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Represents the result of a query for items
    /// </summary>
    [ProtoContract]
    public class ItemsResult
    {
        /// <summary>
        /// The set of items returned based on sorting, paging, etc
        /// </summary>
        /// <value>The items.</value>
        [ProtoMember(1)]
        public BaseItemDto[] Items { get; set; }

        /// <summary>
        /// The total number of records available
        /// </summary>
        /// <value>The total record count.</value>
        [ProtoMember(2)]
        public int TotalRecordCount { get; set; }
    }
}
