using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>Represents and identifier for an external provider.</summary>
    public interface IExternalId
    {
        /// <summary>Gets the name used to identify this provider</summary>
        string Name { get; }

        /// <summary>Gets the unique key to distinguish this provider/type pair. This should be unique across providers.</summary>
        string Key { get; }

        /// <summary>Gets the specific media type for this id.</summary>
        ExternalIdMediaType Type { get; }

        /// <summary>Gets the url format string for this id.</summary>
        string UrlFormatString { get; }

        /// <summary>Determines whether this id supports a given item type.</summary>
        /// <param name="item">The item.</param>
        /// <returns>True if this item is supported, otherwise false.</returns>
        bool Supports(IHasProviderIds item);
    }
}
