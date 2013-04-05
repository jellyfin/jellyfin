using MediaBrowser.Model.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface IDisplayPreferencesManager
    /// </summary>
    public interface IDisplayPreferencesManager
    {
        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>DisplayPreferences.</returns>
        Task<DisplayPreferences> GetDisplayPreferences(Guid displayPreferencesId);

        /// <summary>
        /// Saves display preferences for an item
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveDisplayPreferences(DisplayPreferences displayPreferences, CancellationToken cancellationToken);
    }
}
