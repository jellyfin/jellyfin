using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Connectivity;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    public interface IUserManager
    {
        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <value>The users.</value>
        IEnumerable<User> Users { get; }

        /// <summary>
        /// Gets the active connections.
        /// </summary>
        /// <value>The active connections.</value>
        IEnumerable<ClientConnectionInfo> RecentConnections { get; }

        /// <summary>
        /// Occurs when [playback start].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackStart;

        /// <summary>
        /// Occurs when [playback progress].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;

        /// <summary>
        /// Occurs when [playback stopped].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackStopped;

        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        event EventHandler<GenericEventArgs<User>> UserUpdated;

        /// <summary>
        /// Occurs when [user deleted].
        /// </summary>
        event EventHandler<GenericEventArgs<User>> UserDeleted;

        /// <summary>
        /// Gets a User by Id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>User.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        User GetUserById(Guid id);

        /// <summary>
        /// Authenticates a User and returns a result indicating whether or not it succeeded
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        Task<bool> AuthenticateUser(User user, string password);

        /// <summary>
        /// Logs the user activity.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        Task LogUserActivity(User user, string clientType, string deviceId, string deviceName);

        /// <summary>
        /// Refreshes metadata for each user
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns>Task.</returns>
        Task RefreshUsersMetadata(CancellationToken cancellationToken, bool force = false);

        /// <summary>
        /// Renames the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.ArgumentException"></exception>
        Task RenameUser(User user, string newName);

        /// <summary>
        /// Updates the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.ArgumentException"></exception>
        Task UpdateUser(User user);

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>User.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentException"></exception>
        Task<User> CreateUser(string name);

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        /// <exception cref="System.ArgumentException"></exception>
        Task DeleteUser(User user);

        /// <summary>
        /// Used to report that playback has started for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        void OnPlaybackStart(User user, BaseItem item, string clientType, string deviceId, string deviceName);

        /// <summary>
        /// Used to report playback progress for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task OnPlaybackProgress(User user, BaseItem item, long? positionTicks, string clientType, string deviceId, string deviceName);

        /// <summary>
        /// Used to report that playback has ended for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task OnPlaybackStopped(User user, BaseItem item, long? positionTicks, string clientType, string deviceId, string deviceName);

        /// <summary>
        /// Resets the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        Task ResetPassword(User user);

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>Task.</returns>
        Task ChangePassword(User user, string newPassword);

        /// <summary>
        /// Saves display preferences for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userDataId">The user data id.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveUserData(Guid userId, Guid userDataId, UserItemData userData,
                                    CancellationToken cancellationToken);

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userDataId">The user data id.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        Task<UserItemData> GetUserData(Guid userId, Guid userDataId);

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>DisplayPreferences.</returns>
        Task<DisplayPreferences> GetDisplayPreferences(Guid displayPreferencesId);

        /// <summary>
        /// Saves display preferences for an item
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveDisplayPreferences(DisplayPreferences displayPreferences, CancellationToken cancellationToken);
    }
}
