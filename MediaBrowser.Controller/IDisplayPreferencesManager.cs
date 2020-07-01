using System;
using Jellyfin.Data.Entities;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Manages the storage and retrieval of display preferences.
    /// </summary>
    public interface IDisplayPreferencesManager
    {
        /// <summary>
        /// Gets the display preferences for the user and client.
        /// </summary>
        /// <param name="userId">The user's id.</param>
        /// <param name="client">The client string.</param>
        /// <returns>The associated display preferences.</returns>
        DisplayPreferences GetDisplayPreferences(Guid userId, string client);

        /// <summary>
        /// Saves changes to the provided display preferences.
        /// </summary>
        /// <param name="preferences">The display preferences to save.</param>
        void SaveChanges(DisplayPreferences preferences);
    }
}
