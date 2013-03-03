using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface IItemResolver
    /// </summary>
    public interface IItemResolver
    {
        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        BaseItem ResolvePath(ItemResolveArgs args);
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        ResolverPriority Priority { get; }
    }
}
