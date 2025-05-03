using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Users;

/// <summary>
/// Manages the storage and retrieval of display preferences through Entity Framework.
/// </summary>
public sealed class DisplayPreferencesManager : IDisplayPreferencesManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayPreferencesManager"/> class.
    /// </summary>
    /// <param name="dbContextFactory">The database context factory.</param>
    public DisplayPreferencesManager(IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public DisplayPreferences GetDisplayPreferences(Guid userId, Guid itemId, string client)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var prefs = dbContext.DisplayPreferences
            .Include(pref => pref.HomeSections)
            .FirstOrDefault(pref =>
                pref.UserId.Equals(userId) && pref.Client == client && pref.ItemId.Equals(itemId));

        if (prefs is null)
        {
            prefs = new DisplayPreferences(userId, itemId, client);
            dbContext.DisplayPreferences.Add(prefs);
            dbContext.SaveChanges();
        }

        return prefs;
    }

    /// <inheritdoc />
    public ItemDisplayPreferences GetItemDisplayPreferences(Guid userId, Guid itemId, string client)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var prefs = dbContext.ItemDisplayPreferences
            .FirstOrDefault(pref => pref.UserId.Equals(userId) && pref.ItemId.Equals(itemId) && pref.Client == client);

        if (prefs is null)
        {
            prefs = new ItemDisplayPreferences(userId, Guid.Empty, client);
            dbContext.ItemDisplayPreferences.Add(prefs);
            dbContext.SaveChanges();
        }

        return prefs;
    }

    /// <inheritdoc />
    public IList<ItemDisplayPreferences> ListItemDisplayPreferences(Guid userId, string client)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.ItemDisplayPreferences
            .Where(prefs => prefs.UserId.Equals(userId) && !prefs.ItemId.Equals(default) && prefs.Client == client)
            .ToList();
    }

    /// <inheritdoc />
    public Dictionary<string, string?> ListCustomItemDisplayPreferences(Guid userId, Guid itemId, string client)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.CustomItemDisplayPreferences
            .Where(prefs => prefs.UserId.Equals(userId)
                            && prefs.ItemId.Equals(itemId)
                            && prefs.Client == client)
            .ToDictionary(prefs => prefs.Key, prefs => prefs.Value);
    }

    /// <inheritdoc />
    public void SetCustomItemDisplayPreferences(Guid userId, Guid itemId, string client, Dictionary<string, string?> customPreferences)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.CustomItemDisplayPreferences.Where(prefs => prefs.UserId.Equals(userId)
                            && prefs.ItemId.Equals(itemId)
                            && prefs.Client == client)
                            .ExecuteDelete();

        foreach (var (key, value) in customPreferences)
        {
            dbContext.CustomItemDisplayPreferences
                .Add(new CustomItemDisplayPreferences(userId, itemId, client, key, value));
        }

        dbContext.SaveChanges();
    }

    /// <inheritdoc/>
    public void UpdateDisplayPreferences(DisplayPreferences displayPreferences)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.DisplayPreferences.Attach(displayPreferences).State = EntityState.Modified;
        dbContext.SaveChanges();
    }

    /// <inheritdoc/>
    public void UpdateItemDisplayPreferences(ItemDisplayPreferences itemDisplayPreferences)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.ItemDisplayPreferences.Attach(itemDisplayPreferences).State = EntityState.Modified;
        dbContext.SaveChanges();
    }
}
