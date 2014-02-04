using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
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
        /// Determines whether [is save local metadata enabled].
        /// </summary>
        /// <returns><c>true</c> if [is save local metadata enabled]; otherwise, <c>false</c>.</returns>
        bool IsSaveLocalMetadataEnabled();
    }
}
