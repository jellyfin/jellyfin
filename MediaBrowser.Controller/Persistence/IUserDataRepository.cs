using MediaBrowser.Controller.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides an interface to implement a UserData repository
    /// </summary>
    public interface IUserDataRepository : IRepository
    {
        /// <summary>
        /// Saves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userDataId">The user data id.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveUserData(Guid userId, Guid userDataId, UserItemData userData,
                                    CancellationToken cancellationToken);

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userDataId">The user data id.</param>
        /// <returns>Task{UserItemData}.</returns>
        Task<UserItemData> GetUserData(Guid userId, Guid userDataId);
    }
}
