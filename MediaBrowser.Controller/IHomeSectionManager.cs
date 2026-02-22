using System;
using System.Collections.Generic;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Interface for managing home sections.
    /// </summary>
    public interface IHomeSectionManager
    {
        /// <summary>
        /// Gets all home sections for a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>A list of home section options.</returns>
        IList<HomeSectionOptions> GetHomeSections(Guid userId);

        /// <summary>
        /// Gets a specific home section for a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="sectionId">The section id.</param>
        /// <returns>The home section options, or null if not found.</returns>
        HomeSectionOptions? GetHomeSection(Guid userId, Guid sectionId);

        /// <summary>
        /// Creates a new home section for a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="options">The home section options.</param>
        /// <returns>The id of the newly created section.</returns>
        Guid CreateHomeSection(Guid userId, HomeSectionOptions options);

        /// <summary>
        /// Updates a home section for a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="sectionId">The section id.</param>
        /// <param name="options">The updated home section options.</param>
        /// <returns>True if the section was updated, false if it was not found.</returns>
        bool UpdateHomeSection(Guid userId, Guid sectionId, HomeSectionOptions options);

        /// <summary>
        /// Deletes a home section for a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="sectionId">The section id.</param>
        /// <returns>True if the section was deleted, false if it was not found.</returns>
        bool DeleteHomeSection(Guid userId, Guid sectionId);

        /// <summary>
        /// Saves changes to the database.
        /// </summary>
        void SaveChanges();
    }
}
