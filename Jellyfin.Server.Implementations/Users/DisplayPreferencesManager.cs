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
    private const int MaxSaveAttempts = 3;

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
        var prefs = FindDisplayPreferences(dbContext, userId, itemId, client);
        if (prefs is not null)
        {
            return prefs;
        }

        prefs = new DisplayPreferences(userId, itemId, client);
        dbContext.DisplayPreferences.Add(prefs);
        try
        {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Another request may have stored the preferences between the lookup and the insert, and the unique index
            // rejected this one. Return the stored preferences; if there are none, the insert failed for another reason.
            using var retryContext = _dbContextFactory.CreateDbContext();
            var stored = FindDisplayPreferences(retryContext, userId, itemId, client);
            if (stored is null)
            {
                throw;
            }

            return stored;
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
            prefs = new ItemDisplayPreferences(userId, itemId, client);
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
        // Another request can store one of these keys after this one's delete, and the unique index then rejects the
        // insert. Replacing the set again gives the same result, so the replace is repeated.
        for (var attempt = 1; ; attempt++)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            using var transaction = dbContext.Database.BeginTransaction();
            dbContext.CustomItemDisplayPreferences.Where(prefs => prefs.UserId.Equals(userId)
                                && prefs.ItemId.Equals(itemId)
                                && prefs.Client == client)
                                .ExecuteDelete();

            foreach (var (key, value) in customPreferences)
            {
                dbContext.CustomItemDisplayPreferences
                    .Add(new CustomItemDisplayPreferences(userId, itemId, client, key, value));
            }

            try
            {
                dbContext.SaveChanges();
            }
            catch (DbUpdateException) when (attempt < MaxSaveAttempts)
            {
                continue;
            }

            transaction.Commit();
            return;
        }
    }

    /// <inheritdoc/>
    public void UpdateDisplayPreferences(DisplayPreferences displayPreferences)
    {
        // The caller replaces the home sections on a detached entity, so the context below has never tracked the
        // stored rows. They have to be deleted explicitly or every save only inserts the new set and leaves the
        // previous one behind.
        using var dbContext = _dbContextFactory.CreateDbContext();
        using var transaction = dbContext.Database.BeginTransaction();
        dbContext.Set<HomeSection>()
            .Where(section => section.DisplayPreferencesId == displayPreferences.Id)
            .ExecuteDelete();

        // Tracking the entity through Entry() instead of Attach() keeps the graph out of the context, so the
        // replacement sections are added below rather than being attached as unchanged.
        dbContext.Entry(displayPreferences).State = EntityState.Modified;

        foreach (var homeSection in displayPreferences.HomeSections)
        {
            homeSection.DisplayPreferencesId = displayPreferences.Id;
            dbContext.Set<HomeSection>().Add(homeSection);
        }

        dbContext.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc/>
    public void UpdateItemDisplayPreferences(ItemDisplayPreferences itemDisplayPreferences)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.ItemDisplayPreferences.Attach(itemDisplayPreferences).State = EntityState.Modified;
        dbContext.SaveChanges();
    }

    private static DisplayPreferences? FindDisplayPreferences(JellyfinDbContext dbContext, Guid userId, Guid itemId, string client)
        => dbContext.DisplayPreferences
            .Include(pref => pref.HomeSections)
            .FirstOrDefault(pref => pref.UserId.Equals(userId) && pref.Client == client && pref.ItemId.Equals(itemId));
}
