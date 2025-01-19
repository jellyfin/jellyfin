#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using AudioBook = MediaBrowser.Controller.Entities.AudioBook;
using Book = MediaBrowser.Controller.Entities.Book;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Class UserDataManager.
    /// </summary>
    public class UserDataManager : IUserDataManager
    {
        private readonly ConcurrentDictionary<string, UserItemData> _userData =
            new ConcurrentDictionary<string, UserItemData>(StringComparer.OrdinalIgnoreCase);

        private readonly IServerConfigurationManager _config;
        private readonly IDbContextFactory<JellyfinDbContext> _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataManager"/> class.
        /// </summary>
        /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="repository">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
        public UserDataManager(
            IServerConfigurationManager config,
            IDbContextFactory<JellyfinDbContext> repository)
        {
            _config = config;
            _repository = repository;
        }

        /// <inheritdoc />
        public event EventHandler<UserDataSaveEventArgs>? UserDataSaved;

        /// <inheritdoc />
        public void SaveUserData(User user, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(userData);

            ArgumentNullException.ThrowIfNull(item);

            cancellationToken.ThrowIfCancellationRequested();

            var keys = item.GetUserDataKeys();

            using var dbContext = _repository.CreateDbContext();
            using var transaction = dbContext.Database.BeginTransaction();

            foreach (var key in keys)
            {
                userData.Key = key;
                var userDataEntry = Map(userData, user.Id, item.Id);
                if (dbContext.UserData.Any(f => f.ItemId == userDataEntry.ItemId && f.UserId == userDataEntry.UserId && f.CustomDataKey == userDataEntry.CustomDataKey))
                {
                    dbContext.UserData.Attach(userDataEntry).State = EntityState.Modified;
                }
                else
                {
                    dbContext.UserData.Add(userDataEntry);
                }
            }

            dbContext.SaveChanges();
            transaction.Commit();

            var userId = user.InternalId;
            var cacheKey = GetCacheKey(userId, item.Id);
            _userData.AddOrUpdate(cacheKey, userData, (_, _) => userData);

            UserDataSaved?.Invoke(this, new UserDataSaveEventArgs
            {
                Keys = keys,
                UserData = userData,
                SaveReason = reason,
                UserId = user.Id,
                Item = item
            });
        }

        /// <inheritdoc />
        public void SaveUserData(User user, BaseItem item, UpdateUserItemDataDto userDataDto, UserDataSaveReason reason)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(userDataDto);

            var userData = GetUserData(user, item) ?? throw new InvalidOperationException("UserData should not be null.");

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

        private UserData Map(UserItemData dto, Guid userId, Guid itemId)
        {
            return new UserData()
            {
                ItemId = itemId,
                CustomDataKey = dto.Key,
                Item = null,
                User = null,
                AudioStreamIndex = dto.AudioStreamIndex,
                IsFavorite = dto.IsFavorite,
                LastPlayedDate = dto.LastPlayedDate,
                Likes = dto.Likes,
                PlaybackPositionTicks = dto.PlaybackPositionTicks,
                PlayCount = dto.PlayCount,
                Played = dto.Played,
                Rating = dto.Rating,
                UserId = userId,
                SubtitleStreamIndex = dto.SubtitleStreamIndex,
            };
        }

        private UserItemData Map(UserData dto)
        {
            return new UserItemData()
            {
                Key = dto.CustomDataKey!,
                AudioStreamIndex = dto.AudioStreamIndex,
                IsFavorite = dto.IsFavorite,
                LastPlayedDate = dto.LastPlayedDate,
                Likes = dto.Likes,
                PlaybackPositionTicks = dto.PlaybackPositionTicks,
                PlayCount = dto.PlayCount,
                Played = dto.Played,
                Rating = dto.Rating,
                SubtitleStreamIndex = dto.SubtitleStreamIndex,
            };
        }

        private UserItemData? GetUserData(User user, Guid itemId, List<string> keys)
        {
            var cacheKey = GetCacheKey(user.InternalId, itemId);

            if (_userData.TryGetValue(cacheKey, out var data))
            {
                return data;
            }

            data = GetUserDataInternal(user.Id, itemId, keys);

            if (data is null)
            {
                return new UserItemData()
                {
                    Key = keys[0],
                };
            }

            return _userData.GetOrAdd(cacheKey, data);
        }

        private UserItemData? GetUserDataInternal(Guid userId, Guid itemId, List<string> keys)
        {
            if (keys.Count == 0)
            {
                return null;
            }

            using var context = _repository.CreateDbContext();
            var userData = context.UserData.AsNoTracking().Where(e => e.ItemId == itemId && keys.Contains(e.CustomDataKey) && e.UserId.Equals(userId)).ToArray();

            if (userData.Length > 0)
            {
                var directDataReference = userData.FirstOrDefault(e => e.CustomDataKey == itemId.ToString("N"));
                if (directDataReference is not null)
                {
                    return Map(directDataReference);
                }

                return Map(userData.First());
            }

            return new UserItemData
            {
                Key = keys.Last()!
            };
        }

        /// <summary>
        /// Gets the internal key.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetCacheKey(long internalUserId, Guid itemId)
        {
            return internalUserId.ToString(CultureInfo.InvariantCulture) + "-" + itemId.ToString("N", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public UserItemData? GetUserData(User user, BaseItem item)
        {
            return GetUserData(user, item.Id, item.GetUserDataKeys());
        }

        /// <inheritdoc />
        public UserItemDataDto? GetUserDataDto(BaseItem item, User user)
            => GetUserDataDto(item, null, user, new DtoOptions());

        /// <inheritdoc />
        public UserItemDataDto? GetUserDataDto(BaseItem item, BaseItemDto? itemDto, User user, DtoOptions options)
        {
            var userData = GetUserData(user, item);
            if (userData is null)
            {
                return null;
            }

            var dto = GetUserItemDataDto(userData, item.Id);

            item.FillUserDataDtoValues(dto, userData, itemDto, user, options);
            return dto;
        }

        /// <summary>
        /// Converts a UserItemData to a DTOUserItemData.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="itemId">The reference key to an Item.</param>
        /// <returns>DtoUserItemData.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
        private UserItemDataDto GetUserItemDataDto(UserItemData data, Guid itemId)
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
                ItemId = itemId,
                Key = data.Key
            };
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
}
