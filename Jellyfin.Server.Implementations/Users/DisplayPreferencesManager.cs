#pragma warning disable CA1307
#pragma warning disable CA1309

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesManager"/> class.
        /// </summary>
        /// <param name="dbContextFactory">The database context factory.</param>
        public DisplayPreferencesManager(IDbContextFactory<JellyfinDbContext> dbContextFactory)
        {
            _dbProvider = dbContextFactory;
        }

        /// <inheritdoc />
        public async Task<DisplayPreferences> GetDisplayPreferencesAsync(Guid userId, Guid itemId, string client)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var prefs = await dbContext.DisplayPreferences
                    .Include(pref => pref.HomeSections)
                    .FirstOrDefaultAsync(pref =>
                        pref.UserId.Equals(userId) && string.Equals(pref.Client, client) && pref.ItemId.Equals(itemId)).ConfigureAwait(false);

                if (prefs is null)
                {
                    prefs = new DisplayPreferences(userId, itemId, client);
                    dbContext.DisplayPreferences.Add(prefs);
                }

                return prefs;
            }
        }

        /// <inheritdoc />
        public async Task<ItemDisplayPreferences> GetItemDisplayPreferencesAsync(Guid userId, Guid itemId, string client)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var prefs = await dbContext.ItemDisplayPreferences
                    .FirstOrDefaultAsync(pref => pref.UserId.Equals(userId) && pref.ItemId.Equals(itemId) && string.Equals(pref.Client, client)).ConfigureAwait(false);

                if (prefs is null)
                {
                    prefs = new ItemDisplayPreferences(userId, itemId, client);
                    dbContext.ItemDisplayPreferences.Add(prefs);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }

                return prefs;
            }
        }

        /// <inheritdoc />
        public async Task<IList<ItemDisplayPreferences>> ListItemDisplayPreferencesAsync(Guid userId, string client)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                return await dbContext.ItemDisplayPreferences
                    .AsQueryable()
                    .Where(prefs => prefs.UserId.Equals(userId) && !prefs.ItemId.Equals(default) && string.Equals(prefs.Client, client))
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, string?>> ListCustomItemDisplayPreferencesAsync(Guid userId, Guid itemId, string client)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                return await dbContext.CustomItemDisplayPreferences
                    .Where(prefs => prefs.UserId.Equals(userId)
                                    && prefs.ItemId.Equals(itemId)
                                    && string.Equals(prefs.Client, client))
                    .ToDictionaryAsync(prefs => prefs.Key, prefs => prefs.Value)
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task SetCustomItemDisplayPreferencesAsync(Guid userId, Guid itemId, string client, Dictionary<string, string?> customPreferences)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var existingPrefs = dbContext.CustomItemDisplayPreferences
                    .Where(prefs => prefs.UserId.Equals(userId)
                                    && prefs.ItemId.Equals(itemId)
                                    && string.Equals(prefs.Client, client));
                dbContext.CustomItemDisplayPreferences.RemoveRange(existingPrefs);

                foreach (var (key, value) in customPreferences)
                {
                    dbContext.CustomItemDisplayPreferences
                        .Add(new CustomItemDisplayPreferences(userId, itemId, client, key, value));
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
