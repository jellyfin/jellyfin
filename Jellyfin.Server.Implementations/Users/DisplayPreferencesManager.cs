#pragma warning disable CA1307
#pragma warning disable CA1309

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
        public DisplayPreferences GetDisplayPreferences(Guid userId, Guid itemId, string client)
        {
            var prefs = _dbContext.DisplayPreferences
                .Include(pref => pref.HomeSections)
                .FirstOrDefault(pref =>
                    pref.UserId.Equals(userId) && string.Equals(pref.Client, client) && pref.ItemId.Equals(itemId));

            if (prefs == null)
            {
                prefs = new DisplayPreferences(userId, itemId, client);
                _dbContext.DisplayPreferences.Add(prefs);
            }

            return prefs;
        }

        /// <inheritdoc />
        public ItemDisplayPreferences GetItemDisplayPreferences(Guid userId, Guid itemId, string client)
        {
            var prefs = _dbContext.ItemDisplayPreferences
                .FirstOrDefault(pref => pref.UserId.Equals(userId) && pref.ItemId.Equals(itemId) && string.Equals(pref.Client, client));

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
                .AsQueryable()
                .Where(prefs => prefs.UserId.Equals(userId) && !prefs.ItemId.Equals(default) && string.Equals(prefs.Client, client))
                .ToList();
        }

        /// <inheritdoc />
        public Dictionary<string, string?> ListCustomItemDisplayPreferences(Guid userId, Guid itemId, string client)
        {
            return _dbContext.CustomItemDisplayPreferences
                .AsQueryable()
                .Where(prefs => prefs.UserId.Equals(userId)
                                && prefs.ItemId.Equals(itemId)
                                && string.Equals(prefs.Client, client))
                .ToDictionary(prefs => prefs.Key, prefs => prefs.Value);
        }

        /// <inheritdoc />
        public void SetCustomItemDisplayPreferences(Guid userId, Guid itemId, string client, Dictionary<string, string?> customPreferences)
        {
            var existingPrefs = _dbContext.CustomItemDisplayPreferences
                .AsQueryable()
                .Where(prefs => prefs.UserId.Equals(userId)
                                && prefs.ItemId.Equals(itemId)
                                && string.Equals(prefs.Client, client));
            _dbContext.CustomItemDisplayPreferences.RemoveRange(existingPrefs);

            foreach (var (key, value) in customPreferences)
            {
                _dbContext.CustomItemDisplayPreferences
                    .Add(new CustomItemDisplayPreferences(userId, itemId, client, key, value));
            }
        }

        /// <inheritdoc />
        public void SaveChanges()
        {
            _dbContext.SaveChanges();
        }
    }
}
