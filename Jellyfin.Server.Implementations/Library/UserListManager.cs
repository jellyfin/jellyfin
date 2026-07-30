using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Library;

/// <summary>
/// Manages per-user named lists stored in the Jellyfin database.
/// </summary>
public sealed class UserListManager : IUserListManager, IDisposable
{
    private const int MaxListNameLength = 256;

    private readonly IServerConfigurationManager _config;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;
    private readonly AsyncKeyedLocker<Guid> _mutationLock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserListManager"/> class.
    /// </summary>
    /// <param name="config">The server configuration manager.</param>
    /// <param name="provider">The Jellyfin database provider.</param>
    public UserListManager(
        IServerConfigurationManager config,
        IDbContextFactory<JellyfinDbContext> provider)
    {
        _config = config;
        _provider = provider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserList>> GetListsAsync(Guid userId)
    {
        ThrowIfEmpty(userId, nameof(userId));

        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.UserLists
                .AsNoTracking()
                .Where(list => list.UserId.Equals(userId))
                .OrderBy(list => list.SortIndex)
                .ThenBy(list => list.DateCreated)
                .ThenBy(list => list.Id)
                .ToListAsync()
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<UserList> CreateListAsync(Guid userId, string name, bool autoRemoveWatched)
    {
        ThrowIfEmpty(userId, nameof(userId));
        ValidateName(name);

        using (await _mutationLock.LockAsync(userId).ConfigureAwait(false))
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                if (!await dbContext.Users.AnyAsync(user => user.Id.Equals(userId)).ConfigureAwait(false))
                {
                    throw new ArgumentException("The user does not exist.", nameof(userId));
                }

                var existingLists = await dbContext.UserLists
                    .Where(list => list.UserId.Equals(userId))
                    .OrderBy(list => list.SortIndex)
                    .ThenBy(list => list.DateCreated)
                    .ThenBy(list => list.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);

                if (existingLists.Any(list => list.Name == name))
                {
                    throw CreateDuplicateNameException(name);
                }

                var maximumListCount = _config.Configuration.MaxUserListsPerUser;
                if (existingLists.Count >= maximumListCount)
                {
                    throw new IUserListManager.UserListLimitExceededException(
                        $"The user list limit of {maximumListCount} has been reached.");
                }

                var now = DateTime.UtcNow;
                var list = new UserList
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = name,
                    Kind = UserListKind.Custom,
                    IsDefault = false,
                    AutoRemoveWatched = autoRemoveWatched,
                    SortIndex = GetAppendSortIndex(existingLists, now),
                    DateCreated = now,
                    DateModified = now
                };

                dbContext.UserLists.Add(list);
                try
                {
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (DbUpdateException exception)
                {
                    await ThrowIfDuplicateNameAsync(userId, name, null, exception).ConfigureAwait(false);
                    throw;
                }

                return list;
            }
        }
    }

    /// <inheritdoc />
    public async Task UpdateListAsync(Guid listId, string? name, int? sortIndex, bool? autoRemoveWatched)
    {
        ThrowIfEmpty(listId, nameof(listId));
        if (name is not null)
        {
            ValidateName(name);
        }

        if (sortIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortIndex), sortIndex, "The sort index cannot be negative.");
        }

        using (await _mutationLock.LockAsync(listId).ConfigureAwait(false))
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var list = await GetTrackedListAsync(dbContext, listId).ConfigureAwait(false);
                var changed = false;

                if (name is not null && list.Name != name)
                {
                    if (await dbContext.UserLists
                        .AnyAsync(other => other.UserId.Equals(list.UserId) && !other.Id.Equals(listId) && other.Name == name)
                        .ConfigureAwait(false))
                    {
                        throw CreateDuplicateNameException(name);
                    }

                    list.Name = name;
                    changed = true;
                }

                if (sortIndex is not null && list.SortIndex != sortIndex.Value)
                {
                    list.SortIndex = sortIndex.Value;
                    changed = true;
                }

                if (autoRemoveWatched is not null && list.AutoRemoveWatched != autoRemoveWatched.Value)
                {
                    list.AutoRemoveWatched = autoRemoveWatched.Value;
                    changed = true;
                }

                if (!changed)
                {
                    return;
                }

                list.DateModified = DateTime.UtcNow;
                try
                {
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (DbUpdateException exception)
                {
                    if (name is not null)
                    {
                        await ThrowIfDuplicateNameAsync(list.UserId, name, listId, exception).ConfigureAwait(false);
                    }

                    throw;
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteListAsync(Guid listId)
    {
        ThrowIfEmpty(listId, nameof(listId));

        using (await _mutationLock.LockAsync(listId).ConfigureAwait(false))
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var list = await GetTrackedListAsync(dbContext, listId).ConfigureAwait(false);
                if (list.IsDefault)
                {
                    throw new IUserListManager.DefaultUserListDeletionException(
                        "The default user list cannot be deleted.");
                }

                dbContext.UserLists.Remove(list);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task AddItemAsync(Guid listId, Guid itemId)
    {
        ThrowIfEmpty(listId, nameof(listId));
        ThrowIfEmpty(itemId, nameof(itemId));

        using (await _mutationLock.LockAsync(listId).ConfigureAwait(false))
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var list = await GetTrackedListAsync(dbContext, listId).ConfigureAwait(false);
                var existingItems = await dbContext.UserListItems
                    .Where(listItem => listItem.UserListId.Equals(listId))
                    .OrderBy(listItem => listItem.SortIndex)
                    .ThenBy(listItem => listItem.DateAdded)
                    .ThenBy(listItem => listItem.ItemId)
                    .ToListAsync()
                    .ConfigureAwait(false);

                if (existingItems.Any(listItem => listItem.ItemId.Equals(itemId)))
                {
                    return;
                }

                var maximumItemCount = _config.Configuration.MaxItemsPerUserList;
                if (existingItems.Count >= maximumItemCount)
                {
                    throw new IUserListManager.UserListLimitExceededException(
                        $"The user list item limit of {maximumItemCount} has been reached.");
                }

                if (!await dbContext.BaseItems.AnyAsync(item => item.Id.Equals(itemId)).ConfigureAwait(false))
                {
                    throw new ArgumentException("The library item does not exist.", nameof(itemId));
                }

                var now = DateTime.UtcNow;
                dbContext.UserListItems.Add(new UserListItem
                {
                    UserListId = listId,
                    UserList = list,
                    ItemId = itemId,
                    Item = null,
                    DateAdded = now,
                    SortIndex = GetAppendSortIndex(existingItems)
                });

                list.DateModified = now;
                try
                {
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (DbUpdateException)
                {
                    if (await ContainsListItemAsync(listId, itemId).ConfigureAwait(false))
                    {
                        return;
                    }

                    throw;
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveItemAsync(Guid listId, Guid itemId)
    {
        ThrowIfEmpty(listId, nameof(listId));
        ThrowIfEmpty(itemId, nameof(itemId));

        using (await _mutationLock.LockAsync(listId).ConfigureAwait(false))
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var list = await GetTrackedListAsync(dbContext, listId).ConfigureAwait(false);
                var listItem = await dbContext.UserListItems
                    .FirstOrDefaultAsync(entry => entry.UserListId.Equals(listId) && entry.ItemId.Equals(itemId))
                    .ConfigureAwait(false);
                if (listItem is null)
                {
                    return;
                }

                dbContext.UserListItems.Remove(listItem);
                list.DateModified = DateTime.UtcNow;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task MoveItemAsync(Guid listId, Guid itemId, int newSortIndex)
    {
        ThrowIfEmpty(listId, nameof(listId));
        ThrowIfEmpty(itemId, nameof(itemId));
        if (newSortIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSortIndex), newSortIndex, "The sort index cannot be negative.");
        }

        using (await _mutationLock.LockAsync(listId).ConfigureAwait(false))
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var list = await GetTrackedListAsync(dbContext, listId).ConfigureAwait(false);
                var items = await dbContext.UserListItems
                    .Where(listItem => listItem.UserListId.Equals(listId))
                    .OrderBy(listItem => listItem.SortIndex)
                    .ThenBy(listItem => listItem.DateAdded)
                    .ThenBy(listItem => listItem.ItemId)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var currentIndex = items.FindIndex(listItem => listItem.ItemId.Equals(itemId));
                if (currentIndex < 0)
                {
                    throw new ArgumentException("The item is not in the user list.", nameof(itemId));
                }

                if (newSortIndex >= items.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(newSortIndex),
                        newSortIndex,
                        "The sort index must identify a position in the user list.");
                }

                var movedItem = items[currentIndex];
                items.RemoveAt(currentIndex);
                items.Insert(newSortIndex, movedItem);

                var changed = false;
                for (var index = 0; index < items.Count; index++)
                {
                    if (items[index].SortIndex != index)
                    {
                        items[index].SortIndex = index;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    return;
                }

                list.DateModified = DateTime.UtcNow;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<UserList> GetOrCreateDefaultListAsync(Guid userId)
    {
        ThrowIfEmpty(userId, nameof(userId));

        using (await _mutationLock.LockAsync(userId).ConfigureAwait(false))
        {
            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var existingLists = await dbContext.UserLists
                    .Where(list => list.UserId.Equals(userId))
                    .OrderBy(list => list.SortIndex)
                    .ThenBy(list => list.DateCreated)
                    .ThenBy(list => list.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);
                var defaultList = existingLists.FirstOrDefault(list => list.IsDefault);
                if (defaultList is not null)
                {
                    return defaultList;
                }

                if (!await dbContext.Users.AnyAsync(user => user.Id.Equals(userId)).ConfigureAwait(false))
                {
                    throw new ArgumentException("The user does not exist.", nameof(userId));
                }

                var now = DateTime.UtcNow;
                defaultList = new UserList
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "Watchlist",
                    Kind = UserListKind.Watchlist,
                    IsDefault = true,
                    AutoRemoveWatched = true,
                    SortIndex = GetAppendSortIndex(existingLists, now),
                    DateCreated = now,
                    DateModified = now
                };

                dbContext.UserLists.Add(defaultList);
                try
                {
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (DbUpdateException exception)
                {
                    var concurrentlyCreatedList = await GetDefaultListAsync(userId).ConfigureAwait(false);
                    if (concurrentlyCreatedList is not null)
                    {
                        return concurrentlyCreatedList;
                    }

                    await ThrowIfDuplicateNameAsync(userId, defaultList.Name, null, exception).ConfigureAwait(false);
                    throw;
                }

                return defaultList;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetMembershipAsync(
        Guid userId,
        IReadOnlyList<Guid> itemIds)
    {
        ThrowIfEmpty(userId, nameof(userId));
        ArgumentNullException.ThrowIfNull(itemIds);

        var requestedItemIds = itemIds.Distinct().ToArray();
        var result = requestedItemIds.ToDictionary(
            itemId => itemId,
            _ => (IReadOnlyList<Guid>)Array.Empty<Guid>());
        if (requestedItemIds.Length == 0)
        {
            return result;
        }

        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var memberships = await (
                    from listItem in dbContext.UserListItems
                    join list in dbContext.UserLists on listItem.UserListId equals list.Id
                    where list.UserId.Equals(userId) && requestedItemIds.Contains(listItem.ItemId)
                    orderby list.SortIndex, list.Id
                    select new
                    {
                        listItem.ItemId,
                        ListId = list.Id
                    })
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var membershipGroup in memberships.GroupBy(membership => membership.ItemId))
            {
                result[membershipGroup.Key] = membershipGroup
                    .Select(membership => membership.ListId)
                    .ToList();
            }

            return result;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetListItemIdsAsync(Guid listId)
    {
        ThrowIfEmpty(listId, nameof(listId));

        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.UserListItems
                .Where(listItem => listItem.UserListId.Equals(listId))
                .Select(listItem => listItem.ItemId)
                .ToHashSetAsync()
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetListItemDatesAsync(Guid listId)
    {
        ThrowIfEmpty(listId, nameof(listId));

        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.UserListItems
                .Where(listItem => listItem.UserListId.Equals(listId))
                .ToDictionaryAsync(listItem => listItem.ItemId, listItem => listItem.DateAdded)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Releases resources used to serialize list mutations.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _mutationLock.Dispose();
        _disposed = true;
    }

    private static IUserListManager.DuplicateUserListNameException CreateDuplicateNameException(string name)
    {
        return new IUserListManager.DuplicateUserListNameException(
            $"A user list named '{name}' already exists.");
    }

    private static int GetAppendSortIndex(IReadOnlyList<UserListItem> existingItems)
    {
        if (existingItems.Count == 0)
        {
            return 0;
        }

        var maximumSortIndex = existingItems.Max(item => item.SortIndex);
        if (maximumSortIndex < int.MaxValue)
        {
            return maximumSortIndex + 1;
        }

        for (var index = 0; index < existingItems.Count; index++)
        {
            existingItems[index].SortIndex = index;
        }

        return existingItems.Count;
    }

    private static int GetAppendSortIndex(IReadOnlyList<UserList> existingLists, DateTime dateModified)
    {
        if (existingLists.Count == 0)
        {
            return 0;
        }

        var maximumSortIndex = existingLists.Max(list => list.SortIndex);
        if (maximumSortIndex < int.MaxValue)
        {
            return maximumSortIndex + 1;
        }

        for (var index = 0; index < existingLists.Count; index++)
        {
            if (existingLists[index].SortIndex != index)
            {
                existingLists[index].SortIndex = index;
                existingLists[index].DateModified = dateModified;
            }
        }

        return existingLists.Count;
    }

    private static void ThrowIfEmpty(Guid value, string parameterName)
    {
        if (value.Equals(Guid.Empty))
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > MaxListNameLength)
        {
            throw new ArgumentException(
                $"The list name cannot exceed {MaxListNameLength} characters.",
                nameof(name));
        }
    }

    private async Task<UserList?> GetDefaultListAsync(Guid userId)
    {
        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.UserLists
                .AsNoTracking()
                .Where(list => list.UserId.Equals(userId) && list.IsDefault)
                .OrderBy(list => list.SortIndex)
                .ThenBy(list => list.DateCreated)
                .ThenBy(list => list.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }
    }

    private static async Task<UserList> GetTrackedListAsync(JellyfinDbContext dbContext, Guid listId)
    {
        return await dbContext.UserLists
            .FirstOrDefaultAsync(list => list.Id.Equals(listId))
            .ConfigureAwait(false)
            ?? throw new ArgumentException("The user list does not exist.", nameof(listId));
    }

    private async Task ThrowIfDuplicateNameAsync(
        Guid userId,
        string name,
        Guid? excludedListId,
        DbUpdateException exception)
    {
        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var duplicateQuery = dbContext.UserLists
                .AsNoTracking()
                .Where(list => list.UserId.Equals(userId) && list.Name == name);
            if (excludedListId is not null)
            {
                duplicateQuery = duplicateQuery.Where(list => !list.Id.Equals(excludedListId.Value));
            }

            if (await duplicateQuery.AnyAsync().ConfigureAwait(false))
            {
                throw new IUserListManager.DuplicateUserListNameException(
                    $"A user list named '{name}' already exists.",
                    exception);
            }
        }
    }

    private async Task<bool> ContainsListItemAsync(Guid listId, Guid itemId)
    {
        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.UserListItems
                .AnyAsync(entry => entry.UserListId.Equals(listId) && entry.ItemId.Equals(itemId))
                .ConfigureAwait(false);
        }
    }
}
