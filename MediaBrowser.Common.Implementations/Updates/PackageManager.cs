using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
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

        public async Task InstallPackage(IHttpClient client, ILogger logger, ResourcePool resourcePool, IProgress<double> progress, IApplicationPaths appPaths, PackageVersionInfo package, CancellationToken cancellationToken)
        {
            // Target based on if it is an archive or single assembly
            //  zip archives are assumed to contain directory structures relative to our ProgramDataPath
            var isArchive = string.Equals(Path.GetExtension(package.targetFilename), ".zip", StringComparison.OrdinalIgnoreCase);
            var target = Path.Combine(isArchive ? appPaths.TempUpdatePath : appPaths.PluginsPath, package.targetFilename);

            // Download to temporary file so that, if interrupted, it won't destroy the existing installation
            var tempFile = await client.GetTempFile(package.sourceUrl, resourcePool.Mb, cancellationToken, progress).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Validate with a checksum
            if (package.checksum != Guid.Empty) // support for legacy uploads for now
            {
                using (var crypto = new MD5CryptoServiceProvider())
                using (var stream = new BufferedStream(File.OpenRead(tempFile), 100000))
                {
                    var check = Guid.Parse(BitConverter.ToString(crypto.ComputeHash(stream)).Replace("-", String.Empty));
                    if (check != package.checksum)
                    {
                        throw new ApplicationException(string.Format("Download validation failed for {0}.  Probably corrupted during transfer.", package.name));
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Success - move it to the real target 
            try
            {
                File.Copy(tempFile, target, true);
                File.Delete(tempFile);
            }
            catch (IOException e)
            {
                logger.ErrorException("Error attempting to move file from {0} to {1}", e, tempFile, target);
                throw;
            }

        }

    }
}
