using System.Collections.Generic;

namespace Jellyfin.Controller.Entities
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IItemByName
    {
        IList<BaseItem> GetTaggedItems(InternalItemsQuery query);
    }

    public interface IHasDualAccess : IItemByName
    {
        bool IsAccessedByName { get; }
    }
}
