#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface.
    /// </summary>
    public interface IItemByName
    {
        IReadOnlyList<BaseItem> GetTaggedItems(InternalItemsQuery query);
    }

    public interface IHasDualAccess : IItemByName
    {
        bool IsAccessedByName { get; }
    }
}
