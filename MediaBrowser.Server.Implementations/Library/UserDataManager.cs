using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class UserDataManager
    /// </summary>
    public class UserDataManager : IUserDataManager
    {
        public event EventHandler<UserDataSaveEventArgs> UserDataSaved;

        private readonly ConcurrentDictionary<string, UserItemData> _userData =
            new ConcurrentDictionary<string, UserItemData>(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        public UserDataManager(ILogManager logManager, IServerConfigurationManager config)
        {
            _config = config;
            _logger = logManager.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Gets or sets the repository.
        /// </summary>
        /// <value>The repository.</value>
        public IUserDataRepository Repository { get; set; }

        public async Task SaveUserData(Guid userId, IHasUserData item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var keys = item.GetUserDataKeys();

            foreach (var key in keys)
            {
                try
                {
                    await Repository.SaveUserData(userId, key, userData, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error saving user data", ex);

                    throw;
                }
            }

            var cacheKey = GetCacheKey(userId, item.Id);
            _userData.AddOrUpdate(cacheKey, userData, (k, v) => userData);

            EventHelper.FireEventIfNotNull(UserDataSaved, this, new UserDataSaveEventArgs
            {
                Keys = keys,
                UserData = userData,
                SaveReason = reason,
                UserId = userId,
                Item = item

            }, _logger);
        }

        /// <summary>
        /// Save the provided user data for the given user.  Batch operation. Does not fire any events or update the cache.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SaveAllUserData(Guid userId, IEnumerable<UserItemData> userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await Repository.SaveAllUserData(userId, userData, cancellationToken).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving user data", ex);

                throw;
            }

        }

        /// <summary>
        /// Retrieve all user data for the given user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<UserItemData> GetAllUserData(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            return Repository.GetAllUserData(userId);
        }

        public UserItemData GetUserData(Guid userId, Guid itemId, List<string> keys)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            if (keys.Count == 0)
            {
                throw new ArgumentException("UserData keys cannot be empty.");
            }

            var cacheKey = GetCacheKey(userId, itemId);

            return _userData.GetOrAdd(cacheKey, k => GetUserDataInternal(userId, keys));
        }

        private UserItemData GetUserDataInternal(Guid userId, List<string> keys)
        {
            var userData = Repository.GetUserData(userId, keys);

            if (userData != null)
            {
                return userData;
            }

            if (keys.Count > 0)
            {
                return new UserItemData
                {
                    UserId = userId,
                    Key = keys[0]
                };
            }

            return null;
        }

        /// <summary>
        /// Gets the internal key.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetCacheKey(Guid userId, Guid itemId)
        {
            return userId.ToString("N") + itemId.ToString("N");
        }

        public UserItemData GetUserData(IHasUserData user, IHasUserData item)
        {
            return GetUserData(user.Id, item);
        }

        public UserItemData GetUserData(string userId, IHasUserData item)
        {
            return GetUserData(new Guid(userId), item);
        }

        public UserItemData GetUserData(Guid userId, IHasUserData item)
        {
            return GetUserData(userId, item.Id, item.GetUserDataKeys());
        }

        public async Task<UserItemDataDto> GetUserDataDto(IHasUserData item, User user)
        {
            var userData = GetUserData(user.Id, item);
            var dto = GetUserItemDataDto(userData);

            await item.FillUserDataDtoValues(dto, userData, null, user).ConfigureAwait(false);
            return dto;
        }

        public async Task<UserItemDataDto> GetUserDataDto(IHasUserData item, BaseItemDto itemDto, User user)
        {
            var userData = GetUserData(user.Id, item);
            var dto = GetUserItemDataDto(userData);

            await item.FillUserDataDtoValues(dto, userData, itemDto, user).ConfigureAwait(false);
            return dto;
        }

        /// <summary>
        /// Converts a UserItemData to a DTOUserItemData
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>DtoUserItemData.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        private UserItemDataDto GetUserItemDataDto(UserItemData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

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

        public bool UpdatePlayState(BaseItem item, UserItemData data, long? reportedPositionTicks)
        {
            var playedToCompletion = false;

            var positionTicks = reportedPositionTicks ?? item.RunTimeTicks ?? 0;
            var hasRuntime = item.RunTimeTicks.HasValue && item.RunTimeTicks > 0;

            // If a position has been reported, and if we know the duration
            if (positionTicks > 0 && hasRuntime)
            {
                var pctIn = Decimal.Divide(positionTicks, item.RunTimeTicks.Value) * 100;

                // Don't track in very beginning
                if (pctIn < _config.Configuration.MinResumePct)
                {
                    positionTicks = 0;
                }

                // If we're at the end, assume completed
                else if (pctIn > _config.Configuration.MaxResumePct || positionTicks >= item.RunTimeTicks.Value)
                {
                    positionTicks = 0;
                    data.Played = playedToCompletion = true;
                }

                else
                {
                    // Enforce MinResumeDuration
                    var durationSeconds = TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

                    if (durationSeconds < _config.Configuration.MinResumeDurationSeconds)
                    {
                        positionTicks = 0;
                        data.Played = playedToCompletion = true;
                    }
                }
            }
            else if (!hasRuntime)
            {
                // If we don't know the runtime we'll just have to assume it was fully played
                data.Played = playedToCompletion = true;
                positionTicks = 0;
            }

            if (item is Audio)
            {
                positionTicks = 0;
            }

            data.PlaybackPositionTicks = positionTicks;

            return playedToCompletion;
        }
    }
}
