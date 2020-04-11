using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetPackage
    /// </summary>
    [Route("/Packages/{Name}", "GET", Summary = "Gets a package, by name or assembly guid")]
    [Authenticated]
    public class GetPackage : IReturn<PackageInfo>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The name of the package", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "AssemblyGuid", Description = "The guid of the associated assembly", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AssemblyGuid { get; set; }
    }

    /// <summary>
    /// Class GetPackages
    /// </summary>
    [Route("/Packages", "GET", Summary = "Gets available packages")]
    [Authenticated]
    public class GetPackages : IReturn<PackageInfo[]>
    {
    }

    /// <summary>
    /// Class InstallPackage
    /// </summary>
    [Route("/Packages/Installed/{Name}", "POST", Summary = "Installs a package")]
    [Authenticated(Roles = "Admin")]
    public class InstallPackage : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Package name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "AssemblyGuid", Description = "Guid of the associated assembly", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string AssemblyGuid { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [ApiMember(Name = "Version", Description = "Optional version. Defaults to latest version.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Version { get; set; }
    }

    /// <summary>
    /// Class CancelPackageInstallation
    /// </summary>
    [Route("/Packages/Installing/{Id}", "DELETE", Summary = "Cancels a package installation")]
    [Authenticated(Roles = "Admin")]
    public class CancelPackageInstallation : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Installation Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class PackageService
    /// </summary>
    public class PackageService : BaseApiService
    {
        private readonly IInstallationManager _installationManager;

        public PackageService(
            ILogger<PackageService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IInstallationManager installationManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _installationManager = installationManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPackage request)
        {
            var packages = _installationManager.GetAvailablePackages().GetAwaiter().GetResult();
            var result = _installationManager.FilterPackages(
                packages,
                request.Name,
                string.IsNullOrEmpty(request.AssemblyGuid) ? default : Guid.Parse(request.AssemblyGuid)).FirstOrDefault();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetPackages request)
        {
            IEnumerable<PackageInfo> packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);

            return ToOptimizedResult(packages.ToArray());
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="ResourceNotFoundException"></exception>
        public async Task Post(InstallPackage request)
        {
            var packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);
            var package = _installationManager.GetCompatibleVersions(
                    packages,
                    request.Name,
                    string.IsNullOrEmpty(request.AssemblyGuid) ? Guid.Empty : Guid.Parse(request.AssemblyGuid),
                    string.IsNullOrEmpty(request.Version) ? null : Version.Parse(request.Version)).FirstOrDefault();

            if (package == null)
            {
                throw new ResourceNotFoundException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Package not found: {0}",
                        request.Name));
            }

            await _installationManager.InstallPackage(package);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(CancelPackageInstallation request)
        {
            _installationManager.CancelInstallation(new Guid(request.Id));
        }
    }
}
