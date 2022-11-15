#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

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
