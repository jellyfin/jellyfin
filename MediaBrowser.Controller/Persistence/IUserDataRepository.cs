using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
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
        /// Saves user data for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveUserData(BaseItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Gets user data for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{UserItemData}.</returns>
        IEnumerable<UserItemData> RetrieveUserData(BaseItem item);
    }
}
