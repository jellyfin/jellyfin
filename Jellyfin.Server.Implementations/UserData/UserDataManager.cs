using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.UserData;

/// <summary>
/// Manages the storage and retrieval of <see cref="UserItemData"/> instances.
/// </summary>
public class UserDataManager : IUserDataManager
{
    private readonly IServerConfigurationManager _config;
    private readonly IDbContextFactory<LibraryDbContext> _provider;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataManager"/> class.
    /// </summary>
    /// <param name="config">The <see cref="IServerConfigurationManager"/> to use in the <see cref="UserDataManager"/> instance.</param>
    /// <param name="provider">The Jellyfin database provider.</param>
    /// <param name="userManager">The Jellyfin user manager.</param>
    public UserDataManager(
        IServerConfigurationManager config,
        IDbContextFactory<LibraryDbContext> provider,
        IUserManager userManager)
    {
        _config = config;
        _provider = provider;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public event EventHandler<UserDataSaveEventArgs>? UserDataSaved;

    /// <summary>
    /// Save UserData.
    /// </summary>
    /// <param name="user">The User object.</param>
    /// <param name="item">The BaseItem which relates.</param>
    /// <param name="userData">The info against the base item.</param>
    /// <param name="reason">The reason to save the data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="user"/> or <paramref name="item"/> or <paramref name="userData"/> or <paramref name="cancellationToken"/> is <c>null</c>.
    /// </exception>
    /// <returns>A <see cref="Task"/> to save the userdata against an item.</returns>
    public async Task SaveUserDataAsync(User? user, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userData);

        ArgumentNullException.ThrowIfNull(item);

        ArgumentNullException.ThrowIfNull(user);

        cancellationToken.ThrowIfCancellationRequested();

        var keys = item.GetUserDataKeys();

        var dbContext = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            foreach (var key in keys)
            {
                userData.Key = key;
                var existingUserDataItem = await dbContext.UserDatas.FirstOrDefaultAsync(predicate => predicate.UserId.Equals(user.Id) && predicate.Key == userData.Key, cancellationToken).ConfigureAwait(false);

                if (existingUserDataItem == null)
                {
                    await dbContext.UserDatas.AddAsync(userData, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    dbContext.Entry(existingUserDataItem).CurrentValues.SetValues(userData);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        UserDataSaved?.Invoke(this, new UserDataSaveEventArgs
        {
            Keys = keys,
            UserData = userData,
            SaveReason = reason,
            UserId = user!.Id,
            Item = item
        });
    }

    /// <inheritdoc />
    public void SaveUserData(Guid userId, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(userId);
        SaveUserData(user, item, userData, reason, cancellationToken);
    }

    /// <inheritdoc />
    public void SaveUserData(User? user, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken)
    {
        SaveUserDataAsync(user, item, userData, reason, cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void SaveUserData(User user, BaseItem item, UpdateUserItemDataDto userDataDto, UserDataSaveReason reason)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(reason);
        ArgumentNullException.ThrowIfNull(userDataDto);

        var userData = GetUserData(user, item);

        if (userDataDto.PlaybackPositionTicks.HasValue)
        {
            userData.PlaybackPositionTicks = userDataDto.PlaybackPositionTicks.Value;
        }

        if (userDataDto.PlayCount.HasValue)
        {
            userData.PlayCount = userDataDto.PlayCount.Value;
        }

        if (userDataDto.IsFavorite.HasValue)
        {
            userData.IsFavorite = userDataDto.IsFavorite.Value;
        }

        if (userDataDto.Likes.HasValue)
        {
            userData.Likes = userDataDto.Likes.Value;
        }

        if (userDataDto.Played.HasValue)
        {
            userData.Played = userDataDto.Played.Value;
        }

        if (userDataDto.LastPlayedDate.HasValue)
        {
            userData.LastPlayedDate = userDataDto.LastPlayedDate.Value;
        }

        if (userDataDto.Rating.HasValue)
        {
            userData.Rating = userDataDto.Rating.Value;
        }

        SaveUserData(user, item, userData, reason, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<UserItemData?> GetUserDataAsync(User? user, BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(item);
        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        UserItemData? userItemData;
        await using (dbContext.ConfigureAwait(false))
        {
            userItemData = await dbContext.UserDatas.Where(u => u.UserId.Equals(user.Id) && u.Key == item.Id.ToString()).FirstOrDefaultAsync().ConfigureAwait(false) ?? new UserItemData
            {
                UserId = user.Id,
                Key = item.Id.ToString()
            };
        }

        return userItemData;
    }

    /// <inheritdoc />
    public UserItemData GetUserData(User? user, BaseItem item)
    {
        return GetUserDataAsync(user, item).GetAwaiter().GetResult() ?? new UserItemData
        {
            UserId = user?.Id ?? Guid.Empty
        };
    }

    /// <inheritdoc />
    public UserItemDataDto GetUserDataDto(BaseItem item, User user)
    {
        return GetUserDataDtoAsync(item, null, user, new DtoOptions()).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<UserItemDataDto> GetUserDataDtoAsync(BaseItem item, User user)
    {
        return await GetUserDataDtoAsync(item, null, user, new DtoOptions()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UserItemDataDto> GetUserDataDtoAsync(BaseItem item, BaseItemDto? itemDto, User user, DtoOptions options)
    {
         var userData = await GetUserDataAsync(user, item).ConfigureAwait(false);
         var dto = GetUserItemDataDto(userData);

         item.FillUserDataDtoValues(dto, userData, itemDto, user, options);
         return dto;
    }

    /// <inheritdoc />
    public UserItemDataDto GetUserDataDto(BaseItem item, BaseItemDto? itemDto, User user, DtoOptions options)
    {
        return GetUserDataDtoAsync(item, itemDto, user, options).GetAwaiter().GetResult();
    }

    private UserItemDataDto GetUserItemDataDto(UserItemData? data)
     {
         ArgumentNullException.ThrowIfNull(data);

         return new UserItemDataDto
         {
             IsFavorite = data.IsFavorite,
             Likes = data.Likes,
             PlaybackPositionTicks = data.PlaybackPositionTicks,
             PlayCount = data.PlayCount,
             Rating = data.Rating,
             Played = data.Played,
             LastPlayedDate = data.LastPlayedDate,
             Key = data.Key
         };
     }

    /// <inheritdoc />
    public async Task<List<UserItemData>> GetAllUserData(User user)
    {
        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        IQueryable<UserItemData> userItemDatas;
        await using (dbContext.ConfigureAwait(false))
        {
            userItemDatas = dbContext.UserDatas.Where(entity => entity.UserId.Equals(user.Id));
        }

        return userItemDatas.ToList();
    }

    /// <inheritdoc />
    public bool UpdatePlayState(BaseItem item, UserItemData data, long? reportedPositionTicks)
    {
        var playedToCompletion = false;

        var runtimeTicks = item.GetRunTimeTicksForPlayState();

        var positionTicks = reportedPositionTicks ?? runtimeTicks;
        var hasRuntime = runtimeTicks > 0;

        // If a position has been reported, and if we know the duration
        if (positionTicks > 0 && hasRuntime && item is not AudioBook && item is not Book)
        {
            var pctIn = decimal.Divide(positionTicks, runtimeTicks) * 100;

            if (pctIn < _config.Configuration.MinResumePct)
            {
                // ignore progress during the beginning
                positionTicks = 0;
            }
            else if (pctIn > _config.Configuration.MaxResumePct || positionTicks >= runtimeTicks)
            {
                // mark as completed close to the end
                positionTicks = 0;
                data.Played = playedToCompletion = true;
            }
            else
            {
                // Enforce MinResumeDuration
                var durationSeconds = TimeSpan.FromTicks(runtimeTicks).TotalSeconds;
                if (durationSeconds < _config.Configuration.MinResumeDurationSeconds)
                {
                    positionTicks = 0;
                    data.Played = playedToCompletion = true;
                }
            }
        }
        else if (positionTicks > 0 && hasRuntime && item is AudioBook)
        {
            var playbackPositionInMinutes = TimeSpan.FromTicks(positionTicks).TotalMinutes;
            var remainingTimeInMinutes = TimeSpan.FromTicks(runtimeTicks - positionTicks).TotalMinutes;

            if (playbackPositionInMinutes < _config.Configuration.MinAudiobookResume)
            {
                // ignore progress during the beginning
                positionTicks = 0;
            }
            else if (remainingTimeInMinutes < _config.Configuration.MaxAudiobookResume || positionTicks >= runtimeTicks)
            {
                // mark as completed close to the end
                positionTicks = 0;
                data.Played = playedToCompletion = true;
            }
        }
        else if (!hasRuntime)
        {
            // If we don't know the runtime we'll just have to assume it was fully played
            data.Played = playedToCompletion = true;
            positionTicks = 0;
        }

        if (!item.SupportsPlayedStatus)
        {
            positionTicks = 0;
            data.Played = false;
        }

        if (!item.SupportsPositionTicksResume)
        {
            positionTicks = 0;
        }

        data.PlaybackPositionTicks = positionTicks;

        return playedToCompletion;
    }
}
