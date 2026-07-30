#pragma warning disable CS1591

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixNativePlaystateSyncService : ICustomNetflixNativePlaystateSyncService
{
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<CustomNetflixNativePlaystateSyncService> _logger;

    public CustomNetflixNativePlaystateSyncService(
        IUserDataManager userDataManager,
        ILogger<CustomNetflixNativePlaystateSyncService> logger)
    {
        _userDataManager = userDataManager;
        _logger = logger;
    }

    public Task SyncProgressAsync(
        CustomNetflixProfileDto profile,
        User user,
        BaseItem item,
        WatchProgressRow progress,
        string eventType,
        CancellationToken cancellationToken)
    {
        if (!CustomNetflixNativePlaystateSyncPolicy.ShouldSync(profile))
        {
            return Task.CompletedTask;
        }

        try
        {
            var data = GetOrCreateUserData(user, item);
            var positionTicks = CustomNetflixNativePlaystateSyncPolicy.SecondsToTicks(progress.PositionSeconds);
            _userDataManager.UpdatePlayState(item, data, positionTicks);
            if (progress.Completed)
            {
                data.Played = true;
                data.PlaybackPositionTicks = 0;
                data.LastPlayedDate = progress.LastPlayedAt;
                data.PlayCount = Math.Max(data.PlayCount, 1);
            }

            var reason = eventType is "complete"
                ? UserDataSaveReason.PlaybackFinished
                : UserDataSaveReason.PlaybackProgress;
            _userDataManager.SaveUserData(user, item, data, reason, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync CustomNetflix progress to native Jellyfin playstate for item {ItemId}.", item.Id);
        }

        return Task.CompletedTask;
    }

    public Task SyncPlayedAsync(CustomNetflixProfileDto profile, User user, BaseItem item, bool played, CancellationToken cancellationToken)
    {
        if (!CustomNetflixNativePlaystateSyncPolicy.ShouldSync(profile))
        {
            return Task.CompletedTask;
        }

        try
        {
            if (played)
            {
                item.MarkPlayed(user, DateTime.UtcNow, resetPosition: true);
            }
            else
            {
                item.MarkUnplayed(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync CustomNetflix played state to native Jellyfin playstate for item {ItemId}.", item.Id);
        }

        return Task.CompletedTask;
    }

    private UserItemData GetOrCreateUserData(User user, BaseItem item)
        => _userDataManager.GetUserData(user, item)
            ?? new UserItemData
            {
                Key = item.GetUserDataKeys().First()
            };
}
