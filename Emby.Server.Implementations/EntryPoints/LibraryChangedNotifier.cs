using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints;

/// <summary>
/// A <see cref="IHostedService"/> responsible for notifying users when libraries are updated.
/// </summary>
public sealed class LibraryChangedNotifier : IHostedService, IDisposable
{
    // A batch holds a live reference to every item it names, so it has to stay small enough that a
    // library scan - which changes items faster than any batch window closes - cannot grow it without
    // bound. Reached only by a scan; interactive use closes a batch on the window long before this.
    internal const int MaxBatchSize = 2000;

    private readonly ILibraryManager _libraryManager;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IProviderManager _providerManager;
    private readonly ISessionManager _sessionManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<LibraryChangedNotifier> _logger;

    private readonly Lock _libraryChangedSyncLock = new();
    private readonly Dictionary<Guid, Folder> _foldersAddedTo = [];
    private readonly Dictionary<Guid, Folder> _foldersRemovedFrom = [];
    private readonly Dictionary<Guid, BaseItem> _itemsAdded = [];
    private readonly Dictionary<Guid, BaseItem> _itemsRemoved = [];
    private readonly Dictionary<Guid, BaseItem> _itemsUpdated = [];
    private readonly ConcurrentDictionary<Guid, DateTime> _lastProgressMessageTimes = new();

    private Timer? _libraryUpdateTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryChangedNotifier"/> class.
    /// </summary>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="sessionManager">The <see cref="ISessionManager"/>.</param>
    /// <param name="userManager">The <see cref="IUserManager"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="providerManager">The <see cref="IProviderManager"/>.</param>
    public LibraryChangedNotifier(
        ILibraryManager libraryManager,
        IServerConfigurationManager configurationManager,
        ISessionManager sessionManager,
        IUserManager userManager,
        ILogger<LibraryChangedNotifier> logger,
        IProviderManager providerManager)
    {
        _libraryManager = libraryManager;
        _configurationManager = configurationManager;
        _sessionManager = sessionManager;
        _userManager = userManager;
        _logger = logger;
        _providerManager = providerManager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnLibraryItemAdded;
        _libraryManager.ItemUpdated += OnLibraryItemUpdated;
        _libraryManager.ItemRemoved += OnLibraryItemRemoved;

        _providerManager.RefreshCompleted += OnProviderRefreshCompleted;
        _providerManager.RefreshStarted += OnProviderRefreshStarted;
        _providerManager.RefreshProgress += OnProviderRefreshProgress;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnLibraryItemAdded;
        _libraryManager.ItemUpdated -= OnLibraryItemUpdated;
        _libraryManager.ItemRemoved -= OnLibraryItemRemoved;

        _providerManager.RefreshCompleted -= OnProviderRefreshCompleted;
        _providerManager.RefreshStarted -= OnProviderRefreshStarted;
        _providerManager.RefreshProgress -= OnProviderRefreshProgress;

        return Task.CompletedTask;
    }

    private void OnProviderRefreshProgress(object? sender, GenericEventArgs<Tuple<BaseItem, double>> e)
    {
        var item = e.Argument.Item1;

        if (!EnableRefreshMessage(item))
        {
            return;
        }

        var progress = e.Argument.Item2;

        if (_lastProgressMessageTimes.TryGetValue(item.Id, out var lastMessageSendTime))
        {
            if (progress > 0 && progress < 100 && (DateTime.UtcNow - lastMessageSendTime).TotalMilliseconds < 1000)
            {
                return;
            }
        }

        _lastProgressMessageTimes.AddOrUpdate(item.Id, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);

        var dict = new Dictionary<string, string>();
        dict["ItemId"] = item.Id.ToString("N", CultureInfo.InvariantCulture);
        dict["Progress"] = progress.ToString(CultureInfo.InvariantCulture);

        try
        {
            _sessionManager.SendMessageToAdminSessions(SessionMessageType.RefreshProgress, dict, CancellationToken.None);
        }
        catch
        {
        }

        var collectionFolders = _libraryManager.GetCollectionFolders(item);

        foreach (var collectionFolder in collectionFolders)
        {
            var collectionFolderDict = new Dictionary<string, string>
            {
                ["ItemId"] = collectionFolder.Id.ToString("N", CultureInfo.InvariantCulture),
                ["Progress"] = (collectionFolder.GetRefreshProgress() ?? 0).ToString(CultureInfo.InvariantCulture)
            };

            try
            {
                _sessionManager.SendMessageToAdminSessions(SessionMessageType.RefreshProgress, collectionFolderDict, CancellationToken.None);
            }
            catch
            {
            }
        }
    }

    private void OnProviderRefreshStarted(object? sender, GenericEventArgs<BaseItem> e)
        => OnProviderRefreshProgress(sender, new GenericEventArgs<Tuple<BaseItem, double>>(new Tuple<BaseItem, double>(e.Argument, 0)));

    private void OnProviderRefreshCompleted(object? sender, GenericEventArgs<BaseItem> e)
    {
        OnProviderRefreshProgress(sender, new GenericEventArgs<Tuple<BaseItem, double>>(new Tuple<BaseItem, double>(e.Argument, 100)));

        _lastProgressMessageTimes.TryRemove(e.Argument.Id, out _);
    }

    private static bool EnableRefreshMessage(BaseItem item)
        => item is Folder { IsRoot: false, IsTopParent: true }
            and not (AggregateFolder or UserRootFolder or UserView or Channel);

    private void OnLibraryItemAdded(object? sender, ItemChangeEventArgs e)
        => OnLibraryChange(e.Item, e.Parent, _itemsAdded, _foldersAddedTo);

    private void OnLibraryItemUpdated(object? sender, ItemChangeEventArgs e)
        => OnLibraryChange(e.Item, e.Parent, _itemsUpdated, null);

    private void OnLibraryItemRemoved(object? sender, ItemChangeEventArgs e)
        => OnLibraryChange(e.Item, e.Parent, _itemsRemoved, _foldersRemovedFrom);

    private void OnLibraryChange(BaseItem item, BaseItem parent, Dictionary<Guid, BaseItem> itemsList, Dictionary<Guid, Folder>? foldersList)
    {
        if (!FilterItem(item))
        {
            return;
        }

        lock (_libraryChangedSyncLock)
        {
            // The window runs from the first change of a batch and is never extended. Extending it on
            // every change would keep a library scan's batch open for the whole scan, and the batch
            // holds the items it names alive, so it would grow to the size of the library.
            if (_libraryUpdateTimer is null)
            {
                var updateDuration = TimeSpan.FromSeconds(_configurationManager.Configuration.LibraryUpdateDuration);
                _libraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, updateDuration, Timeout.InfiniteTimeSpan);
            }

            if (foldersList is not null && parent is Folder folder)
            {
                foldersList[folder.Id] = folder;
            }

            itemsList[item.Id] = item;

            // A window long enough to cover a burst still has to give way once the batch is large
            // enough to be worth sending on its own.
            if (_itemsAdded.Count + _itemsRemoved.Count + _itemsUpdated.Count >= MaxBatchSize)
            {
                _libraryUpdateTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            }
        }
    }

    private async void LibraryUpdateTimerCallback(object? state)
    {
        List<Folder> foldersAddedTo;
        List<Folder> foldersRemovedFrom;
        List<BaseItem> itemsUpdated;
        List<BaseItem> itemsAdded;
        List<BaseItem> itemsRemoved;
        lock (_libraryChangedSyncLock)
        {
            foldersAddedTo = _foldersAddedTo.Values.ToList();
            foldersRemovedFrom = _foldersRemovedFrom.Values.ToList();

            itemsUpdated = _itemsUpdated
                .Where(e => !_itemsAdded.ContainsKey(e.Key))
                .Select(e => e.Value)
                .ToList();

            itemsAdded = _itemsAdded.Values.ToList();
            itemsRemoved = _itemsRemoved.Values.ToList();

            if (_libraryUpdateTimer is not null)
            {
                _libraryUpdateTimer.Dispose();
                _libraryUpdateTimer = null;
            }

            _itemsAdded.Clear();
            _itemsRemoved.Clear();
            _itemsUpdated.Clear();
            _foldersAddedTo.Clear();
            _foldersRemovedFrom.Clear();
        }

        if (itemsAdded.Count == 0
            && itemsUpdated.Count == 0
            && itemsRemoved.Count == 0
            && foldersAddedTo.Count == 0
            && foldersRemovedFrom.Count == 0)
        {
            return;
        }

        await SendChangeNotifications(itemsAdded, itemsUpdated, itemsRemoved, foldersAddedTo, foldersRemovedFrom, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task SendChangeNotifications(
        List<BaseItem> itemsAdded,
        List<BaseItem> itemsUpdated,
        List<BaseItem> itemsRemoved,
        List<Folder> foldersAddedTo,
        List<Folder> foldersRemovedFrom,
        CancellationToken cancellationToken)
    {
        var userIds = _sessionManager.Sessions
            .Select(i => i.UserId)
            .Where(i => !i.IsEmpty())
            .Distinct()
            .ToArray();

        foreach (var userId in userIds)
        {
            LibraryUpdateInfo info;

            try
            {
                info = GetLibraryUpdateInfo(itemsAdded, itemsUpdated, itemsRemoved, foldersAddedTo, foldersRemovedFrom, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLibraryUpdateInfo");
                return;
            }

            if (info.IsEmpty)
            {
                continue;
            }

            try
            {
                await _sessionManager.SendMessageToUserSessions(
                        new List<Guid> { userId },
                        SessionMessageType.LibraryChanged,
                        info,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending LibraryChanged message");
            }
        }
    }

    private LibraryUpdateInfo GetLibraryUpdateInfo(
        List<BaseItem> itemsAdded,
        List<BaseItem> itemsUpdated,
        List<BaseItem> itemsRemoved,
        List<Folder> foldersAddedTo,
        List<Folder> foldersRemovedFrom,
        Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        ArgumentNullException.ThrowIfNull(user);

        var newAndRemoved = new List<BaseItem>();
        newAndRemoved.AddRange(foldersAddedTo);
        newAndRemoved.AddRange(foldersRemovedFrom);

        var allUserRootChildren = _libraryManager.GetUserRootFolder()
            .GetChildren(user, true)
            .OfType<Folder>()
            .ToList();

        return new LibraryUpdateInfo
        {
            ItemsAdded = itemsAdded.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user))
                .Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture))
                .Distinct()
                .ToArray(),
            ItemsUpdated = itemsUpdated.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user))
                .Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture))
                .Distinct()
                .ToArray(),
            ItemsRemoved = itemsRemoved.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user, true))
                .Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture))
                .Distinct()
                .ToArray(),
            FoldersAddedTo = foldersAddedTo.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user))
                .Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture))
                .Distinct()
                .ToArray(),
            FoldersRemovedFrom = foldersRemovedFrom.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user))
                .Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture))
                .Distinct()
                .ToArray(),
            CollectionFolders = GetTopParentIds(newAndRemoved, allUserRootChildren).ToArray()
        };
    }

    private static bool FilterItem(BaseItem item)
    {
        if (!item.IsFolder && !item.HasPathProtocol)
        {
            return false;
        }

        if (item is IItemByName && item is not MusicArtist)
        {
            return false;
        }

        return item.SourceType == SourceType.Library;
    }

    private static IEnumerable<string> GetTopParentIds(List<BaseItem> items, List<Folder> allUserRootChildren)
    {
        var list = new List<string>();

        foreach (var item in items)
        {
            // If the physical root changed, return the user root
            if (item is AggregateFolder)
            {
                continue;
            }

            foreach (var folder in allUserRootChildren)
            {
                list.Add(folder.Id.ToString("N", CultureInfo.InvariantCulture));
            }
        }

        return list.Distinct(StringComparer.Ordinal);
    }

    private T[] TranslatePhysicalItemToUserLibrary<T>(T item, User user, bool includeIfNotFound = false)
        where T : BaseItem
    {
        // If the physical root changed, return the user root
        if (item is AggregateFolder)
        {
            return _libraryManager.GetUserRootFolder() is T t ? new[] { t } : Array.Empty<T>();
        }

        // Return it only if it's in the user's library
        if (includeIfNotFound || item.IsVisibleStandalone(user))
        {
            return new[] { item };
        }

        return Array.Empty<T>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _libraryUpdateTimer?.Dispose();
        _libraryUpdateTimer = null;
    }
}
