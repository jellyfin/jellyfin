using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates
{
    public interface IPackageManager
    {
        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="networkManager"></param>
        /// <param name="securityManager"></param>
        /// <param name="resourcePool"></param>
        /// <param name="serializer"></param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        Task<IEnumerable<PackageInfo>> GetAvailablePackages(IHttpClient client,
                                                            INetworkManager networkManager,
                                                            ISecurityManager securityManager,
                                                            ResourcePool resourcePool,
                                                            IJsonSerializer serializer,
                                                            CancellationToken cancellationToken);

        /// <summary>
        /// Installs a package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task InstallPackage(PackageVersionInfo package, CancellationToken cancellationToken);
    }
}
