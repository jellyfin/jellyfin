using MediaBrowser.Controller.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IProviderRepository : IRepository
    {
        /// <summary>
        /// Gets the metadata status.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>MetadataStatus.</returns>
        MetadataStatus GetMetadataStatus(Guid itemId);

        /// <summary>
        /// Saves the metadata status.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveMetadataStatus(MetadataStatus status, CancellationToken cancellationToken);

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task Initialize();
    }
}
