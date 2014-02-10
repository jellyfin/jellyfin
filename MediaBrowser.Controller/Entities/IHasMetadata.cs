using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasMetadata
    /// </summary>
    public interface IHasMetadata : IHasImages
    {
        /// <summary>
        /// Gets the preferred metadata country code.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetPreferredMetadataCountryCode();

        /// <summary>
        /// Gets the date modified.
        /// </summary>
        /// <value>The date modified.</value>
        DateTime DateModified { get; }

        /// <summary>
        /// Gets the locked fields.
        /// </summary>
        /// <value>The locked fields.</value>
        List<MetadataFields> LockedFields { get; }

        /// <summary>
        /// Gets or sets the date last saved.
        /// </summary>
        /// <value>The date last saved.</value>
        DateTime DateLastSaved { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is in mixed folder.
        /// </summary>
        /// <value><c>true</c> if this instance is in mixed folder; otherwise, <c>false</c>.</value>
        bool IsInMixedFolder { get; }

        /// <summary>
        /// Updates to repository.
        /// </summary>
        /// <param name="updateReason">The update reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken);

        /// <summary>
        /// This is called before any metadata refresh and returns ItemUpdateType indictating if changes were made, and what.
        /// </summary>
        /// <returns>ItemUpdateType.</returns>
        ItemUpdateType BeforeMetadataRefresh();
    }
}
