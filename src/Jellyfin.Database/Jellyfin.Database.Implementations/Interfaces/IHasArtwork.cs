using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities.Libraries;

namespace Jellyfin.Database.Implementations.Interfaces
{
    /// <summary>
    /// An interface abstracting an entity that has artwork.
    /// </summary>
    public interface IHasArtwork
    {
        /// <summary>
        /// Gets a collection containing this entity's artwork.
        /// </summary>
        ICollection<Artwork> Artwork { get; }
    }
}
