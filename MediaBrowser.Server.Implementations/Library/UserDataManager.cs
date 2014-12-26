using System.Collections.Generic;
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

        private readonly ConcurrentDictionary<string, UserItemData> _userData = new ConcurrentDictionary<string, UserItemData>();

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

            var key = item.GetUserDataKey();

            try
            {
                await Repository.SaveUserData(userId, key, userData, cancellationToken).ConfigureAwait(false);

                var newValue = userData;

                // Once it succeeds, put it into the dictionary to make it available to everyone else
                _userData.AddOrUpdate(GetCacheKey(userId, key), newValue, delegate { return newValue; });
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving user data", ex);

                throw;
            }

            EventHelper.FireEventIfNotNull(UserDataSaved, this, new UserDataSaveEventArgs
            {
                Key = key,
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

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        public UserItemData GetUserData(Guid userId, string key)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            return _userData.GetOrAdd(GetCacheKey(userId, key), keyName => GetUserDataFromRepository(userId, key));
        }

        public UserItemData GetUserDataFromRepository(Guid userId, string key)
        {
            var data = Repository.GetUserData(userId, key);

            return data;
        }

        /// <summary>
        /// Gets the internal key.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        private string GetCacheKey(Guid userId, string key)
        {
            return userId + key;
        }

        public UserItemDataDto GetUserDataDto(IHasUserData item, User user)
        {
            var userData = GetUserData(user.Id, item.GetUserDataKey());
            var dto = GetUserItemDataDto(userData);

            item.FillUserDataDtoValues(dto, userData, user);

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

        public bool UpdatePlayState(BaseItem item, UserItemData data, long positionTicks)
        {
            var playedToCompletion = false;

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
