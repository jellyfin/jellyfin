using System.Collections.Generic;
using Jellyfin.Data.Entities.Libraries;

namespace Jellyfin.Data.Interfaces
{
    /// <summary>
    /// An interface abstracting an entity that has artwork.
    /// </summary>
    public interface IHasImage
    {
        /// <summary>
        /// Gets a collection containing this entity's artwork.
        /// </summary>
        ICollection<Image> Image { get; }
    }
}
