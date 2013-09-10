using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IItemByName
    {
        ItemByNameCounts ItemCounts { get; set; }

        Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }
    }
}
