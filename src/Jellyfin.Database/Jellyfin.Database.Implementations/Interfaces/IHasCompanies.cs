using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities.Libraries;

namespace Jellyfin.Database.Implementations.Interfaces
{
    /// <summary>
    /// An abstraction representing an entity that has companies.
    /// </summary>
    public interface IHasCompanies
    {
        /// <summary>
        /// Gets a collection containing this entity's companies.
        /// </summary>
        ICollection<Company> Companies { get; }
    }
}
