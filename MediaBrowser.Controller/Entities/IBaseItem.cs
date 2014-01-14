using System;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface ILibraryItem
    /// </summary>
    public interface IBaseItem
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <value>The id.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        string Path { get; }
    }
}
