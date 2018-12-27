using MediaBrowser.Controller.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    public interface IMetadataContainer
    {
        /// <summary>
        /// Refreshes all metadata.
        /// </summary>
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken);
    }
}
