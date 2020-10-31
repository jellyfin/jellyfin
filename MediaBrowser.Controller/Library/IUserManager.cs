#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface IUserManager.
    /// </summary>
    public interface IUserManager
    {
        /// <summary>
        /// Occurs when a user is updated.
        /// </summary>
        event EventHandler<GenericEventArgs<User>> OnUserUpdated;

        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <value>The users.</value>
        IEnumerable<User> Users { get; }

        /// <summary>
        /// Gets the user ids.
        /// </summary>
        /// <value>The users ids.</value>
        IEnumerable<Guid> UsersIds { get; }

        /// <summary>
        /// Initializes the user manager and ensures that a user exists.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets a user by Id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The user with the specified Id, or <c>null</c> if the user doesn't exist.</returns>
        /// <exception cref="ArgumentException"><c>id</c> is an empty Guid.</exception>
        User GetUserById(Guid id);

        /// <summary>
        /// Gets the name of the user by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>User.</returns>
        User GetUserByName(string name);

        /// <summary>
        /// Renames the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException"></exception>
        Task RenameUser(User user, string newName);

        /// <summary>
        /// Updates the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException"></exception>
        void UpdateUser(User user);

        /// <summary>
        /// Updates the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="ArgumentNullException">If user is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If the provided user doesn't exist.</exception>
        /// <returns>A task representing the update of the user.</returns>
        Task UpdateUserAsync(User user);

        /// <summary>
        /// Creates a user with the specified name.
        /// </summary>
        /// <param name="name">The name of the new user.</param>
        /// <returns>The created user.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        /// <exception cref="ArgumentException"></exception>
        Task<User> CreateUserAsync(string name);

        /// <summary>
        /// Deletes the specified user.
        /// </summary>
        /// <param name="userId">The id of the user to be deleted.</param>
        void DeleteUser(Guid userId);

        /// <summary>
        /// Resets the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        Task ResetPassword(User user);

        /// <summary>
        /// Resets the easy password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        void ResetEasyPassword(User user);

        /// <summary>
        /// Changes the password.
        /// </summary>
        Task ChangePassword(User user, string newPassword);

        /// <summary>
        /// Changes the easy password.
        /// </summary>
        void ChangeEasyPassword(User user, string newPassword, string newPasswordSha1);

        /// <summary>
        /// Gets the user dto.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <returns>UserDto.</returns>
        UserDto GetUserDto(User user, string remoteEndPoint = null);

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        Task<User> AuthenticateUser(string username, string password, string passwordSha1, string remoteEndPoint, bool isUserSession);

        /// <summary>
        /// Starts the forgot password process.
        /// </summary>
        /// <param name="enteredUsername">The entered username.</param>
        /// <param name="isInNetwork">if set to <c>true</c> [is in network].</param>
        /// <returns>ForgotPasswordResult.</returns>
        Task<ForgotPasswordResult> StartForgotPasswordProcess(string enteredUsername, bool isInNetwork);

        /// <summary>
        /// Redeems the password reset pin.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        Task<PinRedeemResult> RedeemPasswordResetPin(string pin);

        NameIdPair[] GetAuthenticationProviders();

        NameIdPair[] GetPasswordResetProviders();

        /// <summary>
        /// This method updates the user's configuration.
        /// This is only included as a stopgap until the new API, using this internally is not recommended.
        /// Instead, modify the user object directly, then call <see cref="UpdateUser"/>.
        /// </summary>
        /// <param name="userId">The user's Id.</param>
        /// <param name="config">The request containing the new user configuration.</param>
        /// <returns>A task representing the update.</returns>
        Task UpdateConfigurationAsync(Guid userId, UserConfiguration config);

        /// <summary>
        /// This method updates the user's policy.
        /// This is only included as a stopgap until the new API, using this internally is not recommended.
        /// Instead, modify the user object directly, then call <see cref="UpdateUser"/>.
        /// </summary>
        /// <param name="userId">The user's Id.</param>
        /// <param name="policy">The request containing the new user policy.</param>
        /// <returns>A task representing the update.</returns>
        Task UpdatePolicyAsync(Guid userId, UserPolicy policy);

        /// <summary>
        /// Clears the user's profile image.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>A task representing the clearing of the profile image.</returns>
        Task ClearProfileImageAsync(User user);
    }
}
