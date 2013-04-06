using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class DisplayPreferencesManager
    /// </summary>
    public class DisplayPreferencesManager : IDisplayPreferencesManager
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _display preferences
        /// </summary>
        private readonly ConcurrentDictionary<Guid, Task<DisplayPreferences>> _displayPreferences = new ConcurrentDictionary<Guid, Task<DisplayPreferences>>();

        /// <summary>
        /// Gets the active user repository
        /// </summary>
        /// <value>The display preferences repository.</value>
        public IDisplayPreferencesRepository Repository { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesManager"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DisplayPreferencesManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>DisplayPreferences.</returns>
        public Task<DisplayPreferences> GetDisplayPreferences(Guid displayPreferencesId)
        {
            return _displayPreferences.GetOrAdd(displayPreferencesId, keyName => RetrieveDisplayPreferences(displayPreferencesId));
        }

        /// <summary>
        /// Retrieves the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>DisplayPreferences.</returns>
        private async Task<DisplayPreferences> RetrieveDisplayPreferences(Guid displayPreferencesId)
        {
            var displayPreferences = await Repository.GetDisplayPreferences(displayPreferencesId).ConfigureAwait(false);

            return displayPreferences ?? new DisplayPreferences { Id = displayPreferencesId };
        }

        /// <summary>
        /// Saves display preferences for an item
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task SaveDisplayPreferences(DisplayPreferences displayPreferences, CancellationToken cancellationToken)
        {
            if (displayPreferences == null)
            {
                throw new ArgumentNullException("displayPreferences");
            }
            if (displayPreferences.Id == Guid.Empty)
            {
                throw new ArgumentNullException("displayPreferences.Id");
            }

            try
            {
                await Repository.SaveDisplayPreferences(displayPreferences,
                                                                                        cancellationToken).ConfigureAwait(false);

                var newValue = Task.FromResult(displayPreferences);

                // Once it succeeds, put it into the dictionary to make it available to everyone else
                _displayPreferences.AddOrUpdate(displayPreferences.Id, newValue, delegate { return newValue; });
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving display preferences", ex);

                throw;
            }
        }
    }
}
