using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.Updates;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetPackage
    /// </summary>
    [Route("/Packages/{Name}", "GET")]
    public class GetPackage : IReturn<PackageInfo>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetPackages
    /// </summary>
    [Route("/Packages", "GET")]
    public class GetPackages : IReturn<List<PackageInfo>>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public PackageType? PackageType { get; set; }
    }

    /// <summary>
    /// Class GetPackageVersionUpdates
    /// </summary>
    [Route("/Packages/Updates", "GET")]
    public class GetPackageVersionUpdates : IReturn<List<PackageVersionInfo>>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public PackageType PackageType { get; set; }
    }

    /// <summary>
    /// Class InstallPackage
    /// </summary>
    [Route("/Packages/Installed/{Name}", "POST")]
    public class InstallPackage : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the update class.
        /// </summary>
        /// <value>The update class.</value>
        public PackageVersionClass UpdateClass { get; set; }
    }

    /// <summary>
    /// Class CancelPackageInstallation
    /// </summary>
    [Route("/Packages/Installing/{Id}", "DELETE")]
    public class CancelPackageInstallation : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class PackageService
    /// </summary>
    [Export(typeof(IRestfulService))]
    public class PackageService : BaseRestService
    {
        /// <summary>
        /// Gets or sets the application host.
        /// </summary>
        /// <value>The application host.</value>
        public IApplicationHost ApplicationHost { get; set; }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentException">Unsupported PackageType</exception>
        public object Get(GetPackageVersionUpdates request)
        {
            var kernel = (Kernel)Kernel;

            var result = new List<PackageVersionInfo>();

            if (request.PackageType == PackageType.UserInstalled || request.PackageType == PackageType.All)
            {
                result.AddRange(kernel.InstallationManager.GetAvailablePluginUpdates(false, CancellationToken.None).Result.ToList());
            }

            else if (request.PackageType == PackageType.System || request.PackageType == PackageType.All)
            {
                var updateCheckResult = ApplicationHost.CheckForApplicationUpdate(CancellationToken.None, new Progress<double> { }).Result;

                if (updateCheckResult.IsUpdateAvailable)
                {
                    result.Add(new PackageVersionInfo
                    {
                        versionStr = updateCheckResult.AvailableVersion.ToString()
                    });
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
            var kernel = (Kernel)Kernel;

            var packages = kernel.InstallationManager.GetAvailablePackages(CancellationToken.None, applicationVersion: kernel.ApplicationVersion).Result;

            var result = packages.FirstOrDefault(p => p.name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPackages request)
        {
            var kernel = (Kernel)Kernel;

            var packages = kernel.InstallationManager.GetAvailablePackages(CancellationToken.None, request.PackageType, kernel.ApplicationVersion).Result;

            return ToOptimizedResult(packages.ToList());
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="ResourceNotFoundException"></exception>
        public void Post(InstallPackage request)
        {
            var kernel = (Kernel)Kernel;

            var package = string.IsNullOrEmpty(request.Version) ?
                kernel.InstallationManager.GetLatestCompatibleVersion(request.Name, request.UpdateClass).Result :
                kernel.InstallationManager.GetPackage(request.Name, request.UpdateClass, Version.Parse(request.Version)).Result;

            if (package == null)
            {
                throw new ResourceNotFoundException(string.Format("Package not found: {0}", request.Name));
            }

            Task.Run(() => kernel.InstallationManager.InstallPackage(package, new Progress<double> { }, CancellationToken.None));
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(CancelPackageInstallation request)
        {
            var kernel = (Kernel)Kernel;

            var info = kernel.InstallationManager.CurrentInstallations.FirstOrDefault(i => i.Item1.Id == request.Id);

            if (info != null)
            {
                info.Item2.Cancel();
            }
        }
    }

}
