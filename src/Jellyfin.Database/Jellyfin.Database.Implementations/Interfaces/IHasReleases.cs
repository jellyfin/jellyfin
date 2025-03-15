using System.Collections.Generic;
using Jellyfin.Data.Entities.Libraries;

namespace Jellyfin.Data.Interfaces
{
    /// <summary>
    /// An abstraction representing an entity that has releases.
    /// </summary>
    public interface IHasReleases
    {
        /// <summary>
        /// Gets a collection containing this entity's releases.
        /// </summary>
        ICollection<Release> Releases { get; }
    }
}
