using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints;

/// <summary>
/// Removes completed items from item lists configured to remove watched items automatically.
/// </summary>
public sealed class WatchlistAutoRemover : IHostedService
{
    private readonly ILogger<WatchlistAutoRemover> _logger;
    private readonly IItemCountService _itemCountService;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;
    private readonly IItemListManager _itemListManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistAutoRemover"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemCountService">The item count service.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userDataManager">The user data manager.</param>
    /// <param name="itemListManager">The item list manager.</param>
    /// <param name="userManager">The user manager.</param>
    public WatchlistAutoRemover(
        ILogger<WatchlistAutoRemover> logger,
        IItemCountService itemCountService,
        ILibraryManager libraryManager,
        IUserDataManager userDataManager,
        IItemListManager itemListManager,
        IUserManager userManager)
    {
        _logger = logger;
        _itemCountService = itemCountService;
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
        _itemListManager = itemListManager;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved += OnUserDataSaved;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved -= OnUserDataSaved;
        return Task.CompletedTask;
    }

    private async void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        try
        {
            if (e.SaveReason != UserDataSaveReason.PlaybackFinished || !e.UserData.Played)
            {
                return;
            }

            var autoRemoveListIds = (await _itemListManager.GetListsAsync(e.UserId).ConfigureAwait(false))
                .Where(list => list.AutoRemoveWatched)
                .Select(list => list.Id)
                .ToArray();
            if (autoRemoveListIds.Length == 0)
            {
                return;
            }

            await RemoveFromListsAsync(autoRemoveListIds, e.Item.Id).ConfigureAwait(false);

            if (e.Item is not Episode episode)
            {
                return;
            }

            var series = ResolveSeries(episode);
            var user = _userManager.GetUserById(e.UserId);
            if (series is null || user is null || !IsFullyWatched(series, user))
            {
                return;
            }

            await RemoveFromListsAsync(autoRemoveListIds, series.Id).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error automatically removing watched items from user {UserId}'s lists",
                e.UserId);
        }
    }

    private async Task RemoveFromListsAsync(Guid[] listIds, Guid itemId)
    {
        foreach (var listId in listIds)
        {
            await _itemListManager.RemoveItemAsync(listId, itemId).ConfigureAwait(false);
        }
    }

    private Series? ResolveSeries(Episode episode)
    {
        if (!episode.SeriesId.IsEmpty()
            && _libraryManager.GetItemById<Series>(episode.SeriesId) is Series series)
        {
            return series;
        }

        var seriesId = episode.FindSeriesId();
        if (!seriesId.IsEmpty()
            && _libraryManager.GetItemById<Series>(seriesId) is Series parentSeries)
        {
            return parentSeries;
        }

        return episode.FindParent<Series>();
    }

    private bool IsFullyWatched(Series series, User user)
    {
        var query = new InternalItemsQuery(user);
        var (playedCount, totalCount) = series.LinkedChildren.Length > 0
            ? _itemCountService.GetPlayedAndTotalCountFromLinkedChildren(query, series.Id)
            : _itemCountService.GetPlayedAndTotalCount(query, series.Id);

        return playedCount >= totalCount;
    }
}
