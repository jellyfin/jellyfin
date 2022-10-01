using System;
using System.Collections.Generic;
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
        /// <remarks>
        /// This will create the display preferences if it does not exist, but it will not save automatically.
        /// </remarks>
        /// <param name="userId">The user's id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="client">The client string.</param>
        /// <returns>The associated display preferences.</returns>
        DisplayPreferences GetDisplayPreferences(Guid userId, Guid itemId, string client);

        /// <summary>
        /// Gets the default item display preferences for the user and client.
        /// </summary>
        /// <remarks>
        /// This will create the item display preferences if it does not exist, but it will not save automatically.
        /// </remarks>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="client">The client string.</param>
        /// <returns>The item display preferences.</returns>
        ItemDisplayPreferences GetItemDisplayPreferences(Guid userId, Guid itemId, string client);

        /// <summary>
        /// Gets all of the item display preferences for the user and client.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client string.</param>
        /// <returns>A list of item display preferences.</returns>
        IList<ItemDisplayPreferences> ListItemDisplayPreferences(Guid userId, string client);

        /// <summary>
        /// Gets all of the custom item display preferences for the user and client.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="client">The client string.</param>
        /// <returns>The dictionary of custom item display preferences.</returns>
        Dictionary<string, string?> ListCustomItemDisplayPreferences(Guid userId, Guid itemId, string client);

        /// <summary>
        /// Sets the custom item display preference for the user and client.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="client">The client id.</param>
        /// <param name="customPreferences">A dictionary of custom item display preferences.</param>
        void SetCustomItemDisplayPreferences(Guid userId, Guid itemId, string client, Dictionary<string, string?> customPreferences);

        /// <summary>
        /// Saves changes made to the database.
        /// </summary>
        void SaveChanges();
    }
}
