using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IItemByName : IHasMetadata
    {
        /// <summary>
        /// Gets the tagged items.
        /// </summary>
        /// <param name="inputItems">The input items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems);

        IEnumerable<BaseItem> GetTaggedItems(InternalItemsQuery query);
    }

    public interface IHasDualAccess : IItemByName
    {
        bool IsAccessedByName { get; }
    }
}
