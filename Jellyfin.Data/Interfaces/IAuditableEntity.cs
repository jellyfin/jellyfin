using System;

namespace Jellyfin.Data.Interfaces
{
    /// <summary>
    /// An interface representing an entity that has creation/modification dates.
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// Gets the date this entity was created.
        /// </summary>
        public DateTime DateCreated { get; }

        /// <summary>
        /// Gets or sets the date this entity was modified.
        /// </summary>
        public DateTime DateModified { get; set; }
    }
}
