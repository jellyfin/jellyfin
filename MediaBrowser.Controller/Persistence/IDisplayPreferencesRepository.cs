using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Interface IDisplayPreferencesRepository
    /// </summary>
    public interface IDisplayPreferencesRepository : IRepository
    {
        /// <summary>
        /// Saves display preferences for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveDisplayPrefs(Folder item, CancellationToken cancellationToken);

        /// <summary>
        /// Gets display preferences for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{DisplayPreferences}.</returns>
        IEnumerable<DisplayPreferences> RetrieveDisplayPrefs(Folder item);
    }
}
