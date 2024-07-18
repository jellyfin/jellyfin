using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Represents an identifier for an external provider.
    /// </summary>
    public interface IExternalId
    {
        /// <summary>
        /// Gets the display name of the provider associated with this ID type.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the unique key to distinguish this provider/type pair. This should be unique across providers.
        /// </summary>
        // TODO: This property is not actually unique across the concrete types at the moment. It should be updated to be unique.
        string Key { get; }

        /// <summary>
        /// Gets the specific media type for this id. This is used to distinguish between the different
        /// external id types for providers with multiple ids.
        /// A null value indicates there is no specific media type associated with the external id, or this is the
        /// default id for the external provider so there is no need to specify a type.
        /// </summary>
        /// <remarks>
        /// This can be used along with the <see cref="ProviderName"/> to localize the external id on the client.
        /// </remarks>
        ExternalIdMediaType? Type { get; }

        /// <summary>
        /// Gets the URL format string for this id.
        /// </summary>
        [Obsolete("Obsolete in 10.10, to be removed in 10.11")]
        string? UrlFormatString { get; }

        /// <summary>
        /// Determines whether this id supports a given item type.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if this item is supported, otherwise false.</returns>
        bool Supports(IHasProviderIds item);
    }
}
