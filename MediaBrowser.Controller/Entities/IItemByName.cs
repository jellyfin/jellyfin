using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface for items that represent a name, like a genre or a studio.
    /// </summary>
    public interface IItemByName
    {
        /// <summary>
        /// Gets the items tagged with this name.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The tagged items.</returns>
        IReadOnlyList<BaseItem> GetTaggedItems(InternalItemsQuery query);
    }

    /// <summary>
    /// Interface for by-name items that can also be accessed as a regular library item.
    /// </summary>
    public interface IHasDualAccess : IItemByName
    {
        /// <summary>
        /// Gets a value indicating whether the item is accessed by name.
        /// </summary>
        bool IsAccessedByName { get; }
    }
}
