using System.Collections.Generic;
using Jellyfin.Data.Entities.Libraries;

namespace Jellyfin.Data.Interfaces
{
    /// <summary>
    /// An interface abstracting an entity that has images.
    /// </summary>
    public interface IHasImages
    {
        /// <summary>
        /// Gets a collection containing this entity's images.
        /// </summary>
        ICollection<Image> Images { get; }
    }
}
