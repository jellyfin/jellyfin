using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
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
        /// Gets or sets the date last saved.
        /// </summary>
        /// <value>The date last saved.</value>
        DateTime DateLastSaved { get; set; }

        /// <summary>
        /// Updates to repository.
        /// </summary>
        /// <param name="updateReason">The update reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken);

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool BeforeMetadataRefresh();

        /// <summary>
        /// Gets or sets a value indicating whether this instance is unidentified.
        /// </summary>
        /// <value><c>true</c> if this instance is unidentified; otherwise, <c>false</c>.</value>
        bool IsUnidentified { get; set; }

        /// <summary>
        /// Gets the item identities.
        /// </summary>
        List<IItemIdentity> Identities { get; set; }
    }
}
