using System;

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
        DateTime DateModified { get; set; }

        /// <summary>
        /// Gets or sets the date last saved.
        /// </summary>
        /// <value>The date last saved.</value>
        DateTime DateLastSaved { get; set; }

        SourceType SourceType { get; set; }

        /// <summary>
        /// Gets or sets the date last refreshed.
        /// </summary>
        /// <value>The date last refreshed.</value>
        DateTime DateLastRefreshed { get; set; }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool BeforeMetadataRefresh();

        /// <summary>
        /// Afters the metadata refresh.
        /// </summary>
        void AfterMetadataRefresh();

        /// <summary>
        /// Gets a value indicating whether [supports people].
        /// </summary>
        /// <value><c>true</c> if [supports people]; otherwise, <c>false</c>.</value>
        bool SupportsPeople { get; }

        bool RequiresRefresh();

        bool EnableRefreshOnDateModifiedChange { get; }

        string PresentationUniqueKey { get; set; }

        string GetPresentationUniqueKey();
        string CreatePresentationUniqueKey();
        bool StopRefreshIfLocalMetadataFound { get; }

        int? GetInheritedParentalRatingValue();
        int InheritedParentalRatingValue { get; set; }
    }
}
