using System.Collections.Generic;
using Jellyfin.Data.Entities.Libraries;

namespace Jellyfin.Data.Interfaces
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
