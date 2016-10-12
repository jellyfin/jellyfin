using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Updates;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    public class GetPackages : IReturn<List<PackageInfo>>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "PackageType", Description = "Optional package type filter (System/UserInstalled)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PackageType { get; set; }

        [ApiMember(Name = "TargetSystems", Description = "Optional. Filter by target system type. Allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET", AllowMultiple = true)]
        public string TargetSystems { get; set; }

        [ApiMember(Name = "IsPremium", Description = "Optional. Filter by premium status", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? IsPremium { get; set; }

        [ApiMember(Name = "IsAdult", Description = "Optional. Filter by package that contain adult content.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? IsAdult { get; set; }

        public bool? IsAppStoreEnabled { get; set; }
    }

    /// <summary>
    /// Class GetPackageVersionUpdates
    /// </summary>
    [Route("/Packages/Updates", "GET", Summary = "Gets available package updates for currently installed packages")]
    [Authenticated(Roles = "Admin")]
    public class GetPackageVersionUpdates : IReturn<List<PackageVersionInfo>>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "PackageType", Description = "Package type filter (System/UserInstalled)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PackageType { get; set; }
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

        /// <summary>
        /// Gets or sets the update class.
        /// </summary>
        /// <value>The update class.</value>
        [ApiMember(Name = "UpdateClass", Description = "Optional update class (Dev, Beta, Release). Defaults to Release.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public PackageVersionClass UpdateClass { get; set; }
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
        private readonly IApplicationHost _appHost;

        public PackageService(IInstallationManager installationManager, IApplicationHost appHost)
        {
            _installationManager = installationManager;
            _appHost = appHost;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPackageVersionUpdates request)
        {
            var result = new List<PackageVersionInfo>();

            if (string.Equals(request.PackageType, "UserInstalled", StringComparison.OrdinalIgnoreCase) || string.Equals(request.PackageType, "All", StringComparison.OrdinalIgnoreCase))
            {
                result.AddRange(_installationManager.GetAvailablePluginUpdates(_appHost.ApplicationVersion, false, CancellationToken.None).Result.ToList());
            }

            else if (string.Equals(request.PackageType, "System", StringComparison.OrdinalIgnoreCase) || string.Equals(request.PackageType, "All", StringComparison.OrdinalIgnoreCase))
            {
                var updateCheckResult = _appHost.CheckForApplicationUpdate(CancellationToken.None, new Progress<double>()).Result;

                if (updateCheckResult.IsUpdateAvailable)
                {
                    result.Add(updateCheckResult.Package);
                }
            }

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPackage request)
        {
            var packages = _installationManager.GetAvailablePackages(CancellationToken.None, applicationVersion: _appHost.ApplicationVersion).Result;
            var list = packages.ToList();

            var result = list.FirstOrDefault(p => string.Equals(p.guid, request.AssemblyGuid ?? "none", StringComparison.OrdinalIgnoreCase))
                         ?? list.FirstOrDefault(p => p.name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetPackages request)
        {
            var packages = await _installationManager.GetAvailablePackages(CancellationToken.None, false, request.PackageType, _appHost.ApplicationVersion).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(request.TargetSystems))
            {
                var apps = request.TargetSystems.Split(',').Select(i => (PackageTargetSystem)Enum.Parse(typeof(PackageTargetSystem), i, true));

                packages = packages.Where(p => apps.Contains(p.targetSystem));
            }

            if (request.IsPremium.HasValue)
            {
                packages = packages.Where(p => p.isPremium == request.IsPremium.Value);
            }

            if (request.IsAdult.HasValue)
            {
                packages = packages.Where(p => p.adult == request.IsAdult.Value);
            }

            if (request.IsAppStoreEnabled.HasValue)
            {
                packages = packages.Where(p => p.enableInAppStore == request.IsAppStoreEnabled.Value);
            }

            return ToOptimizedResult(packages.ToList());
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="ResourceNotFoundException"></exception>
        public void Post(InstallPackage request)
        {
            var package = string.IsNullOrEmpty(request.Version) ?
                _installationManager.GetLatestCompatibleVersion(request.Name, request.AssemblyGuid, _appHost.ApplicationVersion, request.UpdateClass).Result :
                _installationManager.GetPackage(request.Name, request.AssemblyGuid, request.UpdateClass, Version.Parse(request.Version)).Result;

            if (package == null)
            {
                throw new ResourceNotFoundException(string.Format("Package not found: {0}", request.Name));
            }

            Task.Run(() => _installationManager.InstallPackage(package, true, new Progress<double>(), CancellationToken.None));
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(CancelPackageInstallation request)
        {
            var info = _installationManager.CurrentInstallations.FirstOrDefault(i => string.Equals(i.Item1.Id, request.Id));

            if (info != null)
            {
                info.Item2.Cancel();
            }
        }
    }

}
