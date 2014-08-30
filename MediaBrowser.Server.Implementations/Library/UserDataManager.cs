using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Entities;
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

        public UserDataManager(ILogManager logManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Gets or sets the repository.
        /// </summary>
        /// <value>The repository.</value>
        public IUserDataRepository Repository { get; set; }

        /// <summary>
        /// Saves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="item">The item.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">userData
        /// or
        /// cancellationToken
        /// or
        /// userId
        /// or
        /// key</exception>
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

            return _userData.GetOrAdd(GetCacheKey(userId, key), keyName => Repository.GetUserData(userId, key));
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
    }
}
