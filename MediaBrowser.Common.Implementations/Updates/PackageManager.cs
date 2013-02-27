using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Implementations.Updates
{
    public class PackageManager : IPackageManager
    {
        public async Task<IEnumerable<PackageInfo>> GetAvailablePackages(IHttpClient client, 
            INetworkManager networkManager, 
            ISecurityManager securityManager, 
            ResourcePool resourcePool, 
            IJsonSerializer serializer, 
            CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string> { { "key", securityManager.SupporterKey }, { "mac", networkManager.GetMacAddress() } };

            using (var json = await client.Post(Constants.Constants.MBAdminUrl + "service/package/retrieveall", data, resourcePool.Mb, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packages = serializer.DeserializeFromStream<List<PackageInfo>>(json).ToList();
                foreach (var package in packages)
                {
                    package.versions = package.versions.Where(v => !string.IsNullOrWhiteSpace(v.sourceUrl))
                        .OrderByDescending(v => v.version).ToList();
                }

                return packages;
            }

        }

        public Task InstallPackage(PackageVersionInfo package, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
