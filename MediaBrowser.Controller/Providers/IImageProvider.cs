using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IImageProvider
    /// </summary>
    public interface IImageProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool Supports(BaseItem item);
    }
}
