#pragma warning disable CA1307

using System;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Manages the storage and retrieval of display preferences through Entity Framework.
    /// </summary>
    public class DisplayPreferencesManager : IDisplayPreferencesManager
    {
        private readonly JellyfinDbProvider _dbProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesManager"/> class.
        /// </summary>
        /// <param name="dbProvider">The Jellyfin db provider.</param>
        public DisplayPreferencesManager(JellyfinDbProvider dbProvider)
        {
            _dbProvider = dbProvider;
        }

        /// <inheritdoc />
        public DisplayPreferences GetDisplayPreferences(Guid userId, string client)
        {
            using var dbContext = _dbProvider.CreateContext();
            var prefs = dbContext.DisplayPreferences
                .Include(pref => pref.HomeSections)
                .FirstOrDefault(pref =>
                    pref.UserId == userId && pref.ItemId == null && string.Equals(pref.Client, client));

            if (prefs == null)
            {
                prefs = new DisplayPreferences(client, userId);
                dbContext.DisplayPreferences.Add(prefs);
            }

            return prefs;
        }

        /// <inheritdoc />
        public void SaveChanges(DisplayPreferences preferences)
        {
            using var dbContext = _dbProvider.CreateContext();
            dbContext.Update(preferences);
            dbContext.SaveChanges();
        }
    }
}
