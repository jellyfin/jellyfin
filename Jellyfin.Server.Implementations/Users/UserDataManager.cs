using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Manages the storage and retrieval of user item data.
    /// </summary>
    public class UserDataManager : IUserDataManager
    {
        private readonly JellyfinDbProvider _provider;
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataManager"/> class.
        /// </summary>
        /// <param name="provider">The Jellyfin db provider.</param>
        /// <param name="config">The server configuration manager.</param>
        public UserDataManager(JellyfinDbProvider provider, IServerConfigurationManager config)
        {
            _provider = provider;
            _config = config;
        }

        /// <inheritdoc/>
        public event EventHandler<UserDataSaveEventArgs> UserDataSaved;

        /// <inheritdoc/>
        public UserItemData GetUserItemData(Guid userId, Guid itemId)
        {
            if (userId == Guid.Empty || itemId == Guid.Empty)
            {
                return null;
            }

            var dbContext = _provider.CreateContext();
            var userData = dbContext.UserItemData
                .FirstOrDefault(entry => entry.UserId == userId && entry.ItemId == itemId);

            if (userData == null)
            {
                userData = new UserItemData { UserId = userId, ItemId = itemId };
                dbContext.Add(userData);

                // TODO: Remove once we implement scoped services
                dbContext.SaveChanges();
            }

            return userData;
        }

        /// <inheritdoc/>
        public void SaveUserItemData(UserItemData itemData, UserDataSaveReason reason, CancellationToken cancellationToken)
        {
            var dbContext = _provider.CreateContext();

            // Because we can't reuse DbContexts within requests, we have to manually reattach the object and mark it as modified.
            // TODO: clean up when we have scoping
            dbContext.Update(itemData).State = EntityState.Modified;
            dbContext.SaveChanges();

            UserDataSaved?.Invoke(this, new UserDataSaveEventArgs
            {
                Keys = new List<string> { itemData.ItemId.ToString() },
                UserData = itemData,
                SaveReason = reason,
                UserId = itemData.UserId,
                ItemId = itemData.ItemId
            });
        }

        /// <inheritdoc/>
        public UserItemDataDto GetUserDataDto(User user, BaseItem item)
        {
            var userData = GetUserItemData(user.Id, item.Id);
            var dto = GetUserItemDataDto(user.Id, item.Id);
            item.FillUserDataDtoValues(dto, userData, null, user, new DtoOptions());

            return dto;
        }

        /// <inheritdoc/>
        public UserItemDataDto GetUserDataDto(User user, BaseItem item, BaseItemDto itemDto, DtoOptions dtoOptions)
        {
            var userData = GetUserItemData(user.Id, item.Id);
            var dto = GetUserDataDto(user, item);
            item.FillUserDataDtoValues(dto, userData, itemDto, user, dtoOptions);

            return dto;
        }

        /// <inheritdoc/>
        public bool UpdatePlayState(BaseItem item, UserItemData data, long? reportedPositionTicks)
        {
            var playedToCompletion = false;
            var runtimeTicks = item.GetRunTimeTicksForPlayState();

            var positionTicks = reportedPositionTicks ?? runtimeTicks;
            var hasRuntime = runtimeTicks > 0;

            // If a position has been reported, and if we know the duration
            if (positionTicks > 0 && hasRuntime)
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
                    data.IsPlayed = playedToCompletion = true;
                }
                else
                {
                    // Enforce MinResumeDuration
                    var durationSeconds = TimeSpan.FromTicks(runtimeTicks).TotalSeconds;
                    if (durationSeconds < _config.Configuration.MinResumeDurationSeconds && !(item is MediaBrowser.Controller.Entities.Book))
                    {
                        positionTicks = 0;
                        data.IsPlayed = playedToCompletion = true;
                    }
                }
            }
            else if (!hasRuntime)
            {
                // If we don't know the runtime we'll just have to assume it was fully played
                data.IsPlayed = playedToCompletion = true;
                positionTicks = 0;
            }

            if (!item.SupportsPlayedStatus)
            {
                positionTicks = 0;
                data.IsPlayed = false;
            }

            if (!item.SupportsPositionTicksResume)
            {
                positionTicks = 0;
            }

            data.PlaybackPositionTicks = positionTicks;

            return playedToCompletion;
        }

        private UserItemDataDto GetUserItemDataDto(Guid userId, Guid itemId)
        {
            var userData = GetUserItemData(userId, itemId);

            return new UserItemDataDto
            {
                Key = itemId.ToString(),
                IsFavorite = userData.IsFavorite,
                Likes = userData.Likes,
                Rating = userData.Rating,
                Played = userData.IsPlayed,
                PlayCount = userData.PlayCount,
                PlaybackPositionTicks = userData.PlaybackPositionTicks,
                LastPlayedDate = userData.LastPlayedDate
            };
        }
    }
}
