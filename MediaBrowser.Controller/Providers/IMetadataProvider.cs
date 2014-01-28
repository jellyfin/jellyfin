using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }

    public interface IMetadataProvider<TItemType> : IMetadataProvider
           where TItemType : IHasMetadata
    {
    }
    
    public interface ILocalMetadataProvider : IMetadataProvider
    {
        /// <summary>
        /// Determines whether [has local metadata] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local metadata] [the specified item]; otherwise, <c>false</c>.</returns>
        bool HasLocalMetadata(IHasMetadata item);
    }

    public interface IRemoteMetadataProvider : IMetadataProvider
    {
    }

    public interface IRemoteMetadataProvider<TItemType> : IMetadataProvider<TItemType>, IRemoteMetadataProvider
        where TItemType : IHasMetadata
    {
        Task<MetadataResult<TItemType>> GetMetadata(ItemId id, CancellationToken cancellationToken);
    }

    public interface ILocalMetadataProvider<TItemType> : IMetadataProvider<TItemType>, ILocalMetadataProvider
         where TItemType : IHasMetadata
    {
        Task<MetadataResult<TItemType>> GetMetadata(string path, CancellationToken cancellationToken);
    }

    public interface IHasChangeMonitor
    {
        /// <summary>
        /// Determines whether the specified date has changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="date">The date.</param>
        /// <returns><c>true</c> if the specified date has changed; otherwise, <c>false</c>.</returns>
        bool HasChanged(IHasMetadata item, DateTime date);
    }

    public enum MetadataProviderType
    {
        Embedded = 0,
        Local = 1,
        Remote = 2
    }

    public class MetadataResult<T>
        where T : IHasMetadata
    {
        public bool HasMetadata { get; set; }
        public T Item { get; set; }
    }

    public class ItemId : IHasProviderIds
    {
        public string Name { get; set; }
        public string MetadataLanguage { get; set; }
        public string MetadataCountryCode { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        public ItemId()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
