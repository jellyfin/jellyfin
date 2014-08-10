using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface IUserManager
    /// </summary>
    public interface IUserManager
    {
        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <value>The users.</value>
        IEnumerable<User> Users { get; }

        /// <summary>
        /// Occurs when [user updated].
        /// </summary>
        event EventHandler<GenericEventArgs<User>> UserUpdated;

        /// <summary>
        /// Occurs when [user deleted].
        /// </summary>
        event EventHandler<GenericEventArgs<User>> UserDeleted;

        event EventHandler<GenericEventArgs<User>> UserCreated;
        event EventHandler<GenericEventArgs<User>> UserConfigurationUpdated;
        event EventHandler<GenericEventArgs<User>> UserPasswordChanged;

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newConfiguration">The new configuration.</param>
        void UpdateConfiguration(User user, UserConfiguration newConfiguration);
        
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
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        Task<bool> AuthenticateUser(string username, string password);

        /// <summary>
        /// Refreshes metadata for each user
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task RefreshUsersMetadata(CancellationToken cancellationToken);

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
    }
}
