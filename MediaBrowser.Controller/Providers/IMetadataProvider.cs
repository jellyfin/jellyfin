using System;
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
        /// Determines whether the specified item has changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="date">The date.</param>
        /// <returns><c>true</c> if the specified item has changed; otherwise, <c>false</c>.</returns>
        bool HasChanged(IHasMetadata item, DateTime date);
    }

    public class MetadataResult<T>
        where T : IHasMetadata
    {
        public bool HasMetadata { get; set; }
        public T Item { get; set; }
    }

}
