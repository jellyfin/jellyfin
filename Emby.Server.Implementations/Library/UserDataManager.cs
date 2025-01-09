using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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
        private readonly IUserManager _userManager;
        private readonly IUserDataRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataManager"/> class.
        /// </summary>
        /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="repository">Instance of the <see cref="IUserDataRepository"/> interface.</param>
        public UserDataManager(
            IServerConfigurationManager config,
            IUserManager userManager,
            IUserDataRepository repository)
        {
            _config = config;
            _userManager = userManager;
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

            var userId = user.InternalId;

            foreach (var key in keys)
            {
                _repository.SaveUserData(userId, key, userData, cancellationToken);
            }

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

        private UserItemData GetUserData(User user, Guid itemId, List<string> keys)
        {
            var userId = user.InternalId;

            var cacheKey = GetCacheKey(userId, itemId);

            return _userData.GetOrAdd(cacheKey, _ => GetUserDataInternal(userId, keys));
        }

        private UserItemData GetUserDataInternal(long internalUserId, List<string> keys)
        {
            var userData = _repository.GetUserData(internalUserId, keys);

            if (userData is not null)
            {
                return userData;
            }

            if (keys.Count > 0)
            {
                return new UserItemData
                {
                    Key = keys[0]
                };
            }

            throw new UnreachableException();
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
        public UserItemData GetUserData(User user, BaseItem item)
        {
            return GetUserData(user, item.Id, item.GetUserDataKeys());
        }

        /// <inheritdoc />
        public UserItemDataDto GetUserDataDto(BaseItem item, User user)
            => GetUserDataDto(item, null, user, new DtoOptions());

        /// <inheritdoc />
        public UserItemDataDto GetUserDataDto(BaseItem item, BaseItemDto? itemDto, User user, DtoOptions options)
        {
            var userData = GetUserData(user, item);
            var dto = GetUserItemDataDto(userData);

            item.FillUserDataDtoValues(dto, userData, itemDto, user, options);
            return dto;
        }

        /// <summary>
        /// Converts a UserItemData to a DTOUserItemData.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>DtoUserItemData.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
        private UserItemDataDto GetUserItemDataDto(UserItemData data)
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
