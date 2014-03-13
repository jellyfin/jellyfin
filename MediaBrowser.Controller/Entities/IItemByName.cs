using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IItemByName
    {
        /// <summary>
        /// Gets the tagged items.
        /// </summary>
        /// <param name="inputItems">The input items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems);
    }

    public interface IHasDualAccess : IItemByName
    {
        bool IsAccessedByName { get; }
    }
}
