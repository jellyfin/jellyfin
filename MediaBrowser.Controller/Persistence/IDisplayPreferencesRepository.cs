using MediaBrowser.Model.Entities;
using System;
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
        /// <param name="userId">The user id.</param>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveDisplayPreferences(Guid userId, Guid displayPreferencesId, DisplayPreferences displayPreferences,
                                    CancellationToken cancellationToken);

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        Task<DisplayPreferences> GetDisplayPreferences(Guid userId, Guid displayPreferencesId);
    }
}
