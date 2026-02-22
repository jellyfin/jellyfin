#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using BitFaster.Caching.Lru;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
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
        private readonly IServerConfigurationManager _config;
        private readonly IDbContextFactory<JellyfinDbContext> _repository;
        private readonly FastConcurrentLru<string, UserItemData> _cache;

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
            _cache = new FastConcurrentLru<string, UserItemData>(Environment.ProcessorCount, _config.Configuration.CacheSize, StringComparer.OrdinalIgnoreCase);
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
            _cache.AddOrUpdate(cacheKey, userData);
            item.UserData = dbContext.UserData.Where(e => e.ItemId == item.Id).AsNoTracking().ToArray(); // rehydrate the cached userdata

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

        private static UserItemData Map(UserData dto)
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

            if (_cache.TryGet(cacheKey, out var data))
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

            return _cache.GetOrAdd(cacheKey, _ => data);
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
            return item.UserData?.Where(e => e.UserId.Equals(user.Id)).Select(Map).FirstOrDefault() ?? new UserItemData()
            {
                Key = item.GetUserDataKeys()[0],
            };
        }

        /// <inheritdoc />
        public Dictionary<Guid, UserItemData> GetUserDataBatch(IReadOnlyList<BaseItem> items, User user)
        {
            var result = new Dictionary<Guid, UserItemData>(items.Count);
            var itemsNeedingQuery = new List<(BaseItem Item, List<string> Keys)>();

            // First, check cache for each item
            foreach (var item in items)
            {
                var cacheKey = GetCacheKey(user.InternalId, item.Id);
                if (_cache.TryGet(cacheKey, out var cachedData))
                {
                    result[item.Id] = cachedData;
                }
                else
                {
                    // Check if item has UserData already loaded
                    var userData = item.UserData?.Where(e => e.UserId.Equals(user.Id)).Select(Map).FirstOrDefault();
                    if (userData is not null)
                    {
                        result[item.Id] = userData;
                        _cache.AddOrUpdate(cacheKey, userData);
                    }
                    else
                    {
                        var keys = item.GetUserDataKeys();
                        itemsNeedingQuery.Add((item, keys));
                    }
                }
            }

            // If all items were in cache or already loaded, return early
            if (itemsNeedingQuery.Count == 0)
            {
                return result;
            }

            // Build a single query for all missing items
            var allItemIds = itemsNeedingQuery.Select(x => x.Item.Id).ToList();
            var allKeys = itemsNeedingQuery.SelectMany(x => x.Keys).Distinct().ToList();

            if (allKeys.Count > 0)
            {
                using var context = _repository.CreateDbContext();
                var userDataArray = context.UserData
                    .AsNoTracking()
                    .Where(e => allItemIds.Contains(e.ItemId) && allKeys.Contains(e.CustomDataKey) && e.UserId.Equals(user.Id))
                    .ToArray();

                // Group by item ID
                var userDataByItem = userDataArray.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToArray());

                // Process each item that needed querying
                foreach (var (item, keys) in itemsNeedingQuery)
                {
                    UserItemData userData;
                    if (userDataByItem.TryGetValue(item.Id, out var itemUserData) && itemUserData.Length > 0)
                    {
                        // Prefer direct reference by item ID
                        var directDataReference = itemUserData.FirstOrDefault(e => e.CustomDataKey == item.Id.ToString("N"));
                        userData = directDataReference is not null ? Map(directDataReference) : Map(itemUserData.First());
                    }
                    else
                    {
                        // No user data found, create default
                        userData = new UserItemData { Key = keys.Count > 0 ? keys[0] : string.Empty };
                    }

                    result[item.Id] = userData;
                    var cacheKey = GetCacheKey(user.InternalId, item.Id);
                    _cache.AddOrUpdate(cacheKey, userData);
                }
            }

            return result;
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
                else if (pctIn > _config.Configuration.MaxResumePct || positionTicks >= (runtimeTicks - TimeSpan.TicksPerSecond))
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
