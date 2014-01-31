using System;

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

    public interface IHasOrder
    {
        int Order { get; }
    }

    public class MetadataResult<T>
        where T : IHasMetadata
    {
        public bool HasMetadata { get; set; }
        public T Item { get; set; }
    }

}
