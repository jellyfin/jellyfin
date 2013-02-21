using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides a base interface for all the repository interfaces
    /// </summary>
    public interface IRepository : IDisposable
    {
        /// <summary>
        /// Opens the connection to the repository
        /// </summary>
        /// <returns>Task.</returns>
        Task Initialize();

        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }
}
