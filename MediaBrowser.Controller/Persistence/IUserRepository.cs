using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides an interface to implement a User repository
    /// </summary>
    public interface IUserRepository : IRepository
    {
        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        void DeleteUser(User user);

        /// <summary>
        /// Retrieves all users.
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        List<User> RetrieveAllUsers();

        void CreateUser(User user);
        void UpdateUser(User user);
    }
}
