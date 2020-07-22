using System;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller;

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
            var dbContext = _dbProvider.CreateContext();
            var user = dbContext.Users.Find(userId);
#pragma warning disable CA1307
            var prefs = user.DisplayPreferences.FirstOrDefault(pref => string.Equals(pref.Client, client));

            if (prefs == null)
            {
                prefs = new DisplayPreferences(client, userId);
                user.DisplayPreferences.Add(prefs);
            }

            return prefs;
        }

        /// <inheritdoc />
        public void SaveChanges(DisplayPreferences preferences)
        {
            var dbContext = _dbProvider.CreateContext();
            dbContext.Update(preferences);
            dbContext.SaveChanges();
        }
    }
}
