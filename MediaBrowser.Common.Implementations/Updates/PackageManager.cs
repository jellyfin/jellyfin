using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.Updates
{
    public class PackageManager : IPackageManager
    {
        private readonly ISecurityManager _securityManager;
        private readonly INetworkManager _networkManager;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageManager" /> class.
        /// </summary>
        /// <param name="securityManager">The security manager.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        public PackageManager(ISecurityManager securityManager, INetworkManager networkManager, IHttpClient httpClient, IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _securityManager = securityManager;
            _networkManager = networkManager;
            _httpClient = httpClient;
            _appPaths = applicationPaths;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        public async Task<IEnumerable<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string> { { "key", _securityManager.SupporterKey }, { "mac", _networkManager.GetMacAddress() } };

            using (var json = await _httpClient.Post(Constants.Constants.MBAdminUrl + "service/package/retrieveall", data, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packages = _jsonSerializer.DeserializeFromStream<List<PackageInfo>>(json).ToList();
                foreach (var package in packages)
                {
                    package.versions = package.versions.Where(v => !string.IsNullOrWhiteSpace(v.sourceUrl))
                        .OrderByDescending(v => v.version).ToList();
                }

                return packages;
            }

        }

        public async Task InstallPackage(IProgress<double> progress, PackageVersionInfo package, CancellationToken cancellationToken)
        {
            // Target based on if it is an archive or single assembly
            //  zip archives are assumed to contain directory structures relative to our ProgramDataPath
            var isArchive = string.Equals(Path.GetExtension(package.targetFilename), ".zip", StringComparison.OrdinalIgnoreCase);
            var target = Path.Combine(isArchive ? _appPaths.TempUpdatePath : _appPaths.PluginsPath, package.targetFilename);

            // Download to temporary file so that, if interrupted, it won't destroy the existing installation
            var tempFile = await _httpClient.GetTempFile(package.sourceUrl, cancellationToken, progress).ConfigureAwait(false);

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
                _logger.ErrorException("Error attempting to move file from {0} to {1}", e, tempFile, target);
                throw;
            }

        }

    }
}
