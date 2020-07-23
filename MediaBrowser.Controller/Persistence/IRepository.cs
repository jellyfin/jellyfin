using System;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides a base interface for all the repository interfaces.
    /// </summary>
    public interface IRepository : IDisposable
    {
        /// <summary>
        /// Gets the name of the repository.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }
}
