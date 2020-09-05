#pragma warning disable CA1307

using System;
using System.Collections.Generic;
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
        private readonly JellyfinDb _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesManager"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        public DisplayPreferencesManager(JellyfinDb dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public DisplayPreferences GetDisplayPreferences(Guid userId, string client)
        {
            var prefs = _dbContext.DisplayPreferences
                .Include(pref => pref.HomeSections)
                .FirstOrDefault(pref =>
                    pref.UserId == userId && string.Equals(pref.Client, client));

            if (prefs == null)
            {
                prefs = new DisplayPreferences(userId, client);
                _dbContext.DisplayPreferences.Add(prefs);
            }

            return prefs;
        }

        /// <inheritdoc />
        public ItemDisplayPreferences GetItemDisplayPreferences(Guid userId, Guid itemId, string client)
        {
            var prefs = _dbContext.ItemDisplayPreferences
                .FirstOrDefault(pref => pref.UserId == userId && pref.ItemId == itemId && string.Equals(pref.Client, client));

            if (prefs == null)
            {
                prefs = new ItemDisplayPreferences(userId, Guid.Empty, client);
                _dbContext.ItemDisplayPreferences.Add(prefs);
            }

            return prefs;
        }

        /// <inheritdoc />
        public IList<ItemDisplayPreferences> ListItemDisplayPreferences(Guid userId, string client)
        {
            return _dbContext.ItemDisplayPreferences
                .Where(prefs => prefs.UserId == userId && prefs.ItemId != Guid.Empty && string.Equals(prefs.Client, client))
                .ToList();
        }

        /// <inheritdoc />
        public void SaveChanges()
        {
            _dbContext.SaveChanges();
        }
    }
}
