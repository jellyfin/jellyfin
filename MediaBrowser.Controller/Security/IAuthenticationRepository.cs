using MediaBrowser.Model.Querying;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Security
{
    public interface IAuthenticationRepository
    {
        /// <summary>
        /// Creates the specified information.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task Create(AuthenticationInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the specified information.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task Update(AuthenticationInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult{AuthenticationInfo}.</returns>
        QueryResult<AuthenticationInfo> Get(AuthenticationInfoQuery query);

        /// <summary>
        /// Gets the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>AuthenticationInfo.</returns>
        AuthenticationInfo Get(string id);
    }
}
