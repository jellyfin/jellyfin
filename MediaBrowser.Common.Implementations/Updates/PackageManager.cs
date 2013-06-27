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

        /// <summary>
        /// Get all available packages including registration information.
        /// Use this for the plug-in catalog to provide all information for this installation.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string> { { "key", _securityManager.SupporterKey }, { "mac", _networkManager.GetMacAddress() } };

            using (var json = await _httpClient.Post(Constants.Constants.MbAdminUrl + "service/package/retrieveall", data, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packages = _jsonSerializer.DeserializeFromStream<List<PackageInfo>>(json).ToList();

                return FilterVersions(packages);
            }

        }

        /// <summary>
        /// Get all available packages using the static file resource.
        /// Use this for update checks as it will be much less taxing on the server and can be cached.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PackageInfo>> GetAvailablePackagesStatic(CancellationToken cancellationToken)
        {
            using (var json = await _httpClient.Get(Constants.Constants.MbAdminUrl + "service/MB3Packages.json", cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packages = _jsonSerializer.DeserializeFromStream<List<PackageInfo>>(json).ToList();

                return FilterVersions(packages);
            }
        }

        private IEnumerable<PackageInfo> FilterVersions(List<PackageInfo> original)
        {
                foreach (var package in original)
                {
                    package.versions = package.versions.Where(v => !string.IsNullOrWhiteSpace(v.sourceUrl))
                        .OrderByDescending(v => v.version).ToList();
                }

                return original;
        }

        public async Task InstallPackage(IProgress<double> progress, PackageVersionInfo package, CancellationToken cancellationToken)
        {
            // Target based on if it is an archive or single assembly
            //  zip archives are assumed to contain directory structures relative to our ProgramDataPath
            var isArchive = string.Equals(Path.GetExtension(package.targetFilename), ".zip", StringComparison.OrdinalIgnoreCase);
            var target = Path.Combine(isArchive ? _appPaths.TempUpdatePath : _appPaths.PluginsPath, package.targetFilename);

            // Download to temporary file so that, if interrupted, it won't destroy the existing installation
            var tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = package.sourceUrl,
                CancellationToken = cancellationToken,
                Progress = progress

            }).ConfigureAwait(false);

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
                //If it is an archive - write out a version file so we know what it is
                if (isArchive)
                {
                    File.WriteAllText(target+".ver", package.versionStr);
                }
            }
            catch (IOException e)
            {
                _logger.ErrorException("Error attempting to move file from {0} to {1}", e, tempFile, target);
                throw;
            }

            try
            {
                File.Delete(tempFile);
            }
            catch (IOException e)
            {
                // Don't fail because of this
                _logger.ErrorException("Error deleting temp file {0]", e, tempFile);
            }
        }

    }
}
