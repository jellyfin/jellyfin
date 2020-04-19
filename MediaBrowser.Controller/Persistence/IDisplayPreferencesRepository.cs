using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Interface IDisplayPreferencesRepository.
    /// </summary>
    public interface IDisplayPreferencesRepository : IRepository
    {
        /// <summary>
        /// Saves display preferences for an item.
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveDisplayPreferences(
            DisplayPreferences displayPreferences,
            string userId,
            string client,
            CancellationToken cancellationToken);

        /// <summary>
        /// Saves all display preferences for a user.
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveAllDisplayPreferences(
            IEnumerable<DisplayPreferences> displayPreferences,
            Guid userId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        DisplayPreferences GetDisplayPreferences(string displayPreferencesId, string userId, string client);

        /// <summary>
        /// Gets all display preferences for the given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        IEnumerable<DisplayPreferences> GetAllDisplayPreferences(Guid userId);
    }
}
