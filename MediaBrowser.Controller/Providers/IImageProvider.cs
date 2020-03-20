using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IImageProvider.
    /// </summary>
    public interface IImageProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Supports the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the provider supports the item.</returns>
        bool Supports(BaseItem item);
    }
}
