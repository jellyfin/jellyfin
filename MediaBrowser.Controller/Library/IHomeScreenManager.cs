using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// IHomeScreenManager interface.
    /// </summary>
    public interface IHomeScreenManager
    {
        /// <summary>
        /// Register a home screen section with the <see cref="IHomeScreenManager"/>.
        /// </summary>
        /// <typeparam name="T">A class that implements <see cref="IHomeScreenSection"/>.</typeparam>
        void RegisterResultsDelegate<T>() where T : IHomeScreenSection;

        /// <summary>
        /// Get the registered <see cref="IHomeScreenSection"/> instances.
        /// </summary>
        /// <returns>Array of <see cref="IHomeScreenSection"/></returns>
        IEnumerable<IHomeScreenSection> GetSectionTypes();

        /// <summary>
        /// Get the results for a particular section type.
        /// </summary>
        /// <param name="key">The section type identifier.</param>
        /// <param name="payload">The payload to pass to the section to adjust its results.</param>
        /// <returns>Array of results.</returns>
        QueryResult<BaseItemDto> InvokeResultsDelegate(string key, HomeScreenSectionPayload payload);

        /// <summary>
        /// Gets whether the user has the Modular Home feature enabled.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>Enabled state</returns>
        bool GetUserFeatureEnabled(Guid userId);

        /// <summary>
        /// Sets the state for whether the user has the Modular Home feature enabled or not.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="enabled">Is the feature enabled or not?</param>
        void SetUserFeatureEnabled(Guid userId, bool enabled);

        /// <summary>
        /// Get the user's Modular Home settings for which sections are enabled.
        /// </summary>
        /// <param name="userId">The user.</param>
        /// <returns>The user's settings.</returns>
        ModularHomeUserSettings? GetUserSettings(Guid userId);

        /// <summary>
        /// Updates the user's Modular Home settings.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="userSettings">The settings for the user.</param>
        /// <returns></returns>
        bool UpdateUserSettings(Guid userId, ModularHomeUserSettings userSettings);
    }

    /// <summary>
    /// Interface describing a Home Screen section for Modular Home.
    /// </summary>
    public interface IHomeScreenSection
    {
        /// <summary>
        /// Section identifier.
        /// </summary>
        public string? Section { get; }

        /// <summary>
        /// Text to display on frontend.
        /// </summary>
        public string? DisplayText { get; set; }

        /// <summary>
        /// Limit of the number of these sections a single user can see at once. (Usually 1)
        /// </summary>
        public int? Limit { get; }

        /// <summary>
        /// If set, will tell the frontend where the header for this section should navigate to.
        /// </summary>
        public string? Route { get; }

        /// <summary>
        /// Any additional data the section uses.
        /// </summary>
        public string? AdditionalData { get; set; }

        /// <summary>
        /// Get the results for the section.
        /// </summary>
        /// <param name="payload">The data for the section to determine what results to provider.</param>
        /// <returns>Array of results.</returns>
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload);
    }

    /// <summary>
    /// Model for <see cref="IHomeScreenSection"/> used for getting section info.
    /// </summary>
    public class HomeScreenSectionInfo
    {
        /// <summary>
        /// Section ID
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// DisplayText
        /// </summary>
        public string? DisplayText { get; set; }

        /// <summary>
        /// Display Limit
        /// </summary>
        public int Limit { get; set; } = 1;

        /// <summary>
        /// URL to navigation to
        /// </summary>
        public string? Route { get; set; }

        /// <summary>
        /// Any additional data.
        /// </summary>
        public string? AdditionalData { get; set; }

    }

    /// <summary>
    /// UserSettings model for Modular Home.
    /// </summary>
    public class ModularHomeUserSettings
    {
        /// <summary>
        /// The UserID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// What sections are enabled.
        /// </summary>
        public List<string> EnabledSections { get; set; } = new List<string>();
    }

    /// <summary>
    /// Extensions for <see cref="IHomeScreenSection"/>.
    /// </summary>
    public static class HomeScreenSectionExtensions
    {
        /// <summary>
        /// Converts the <see cref="IHomeScreenSection"/> to a <see cref="HomeScreenSectionInfo"/>.
        /// </summary>
        /// <param name="section"><see cref="IHomeScreenSection"/> instance to convert.</param>
        /// <returns>Converted <see cref="HomeScreenSectionInfo"/>.</returns>
        public static HomeScreenSectionInfo AsInfo(this IHomeScreenSection section)
        {
            return new HomeScreenSectionInfo
            {
                Section = section.Section,
                DisplayText = section.DisplayText,
                AdditionalData = section.AdditionalData,
                Route = section.Route,
                Limit = section.Limit ?? 1
            };
        }
    }
}
