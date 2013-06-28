using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Updates
{
    public interface IPackageManager
    {
        /// <summary>
        /// Gets all available packages dynamically.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        Task<IEnumerable<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken);

        /// <summary>
        /// Gets all available packages from a static resource.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        Task<IEnumerable<PackageInfo>> GetAvailablePackagesWithoutRegistrationInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Installs a package.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="package">The package.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task InstallPackage(IProgress<double> progress, PackageVersionInfo package, CancellationToken cancellationToken);
    }
}
