#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Package Controller.
    /// </summary>
    [Route("Packages")]
    [Authorize]
    public class PackageController : BaseJellyfinApiController
    {
        private readonly IInstallationManager _installationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageController"/> class.
        /// </summary>
        /// <param name="installationManager">Instance of <see cref="IInstallationManager"/>Installation Manager.</param>
        public PackageController(IInstallationManager installationManager)
        {
            _installationManager = installationManager;
        }

        /// <summary>
        /// Gets a package, by name or assembly guid.
        /// </summary>
        /// <param name="name">The name of the package.</param>
        /// <param name="assemblyGuid">The guid of the associated assembly.</param>
        /// <returns>Package info.</returns>
        [HttpGet("/{Name}")]
        [ProducesResponseType(typeof(PackageInfo), StatusCodes.Status200OK)]
        public async Task<ActionResult<PackageInfo>> GetPackageInfo(
            [FromRoute] [Required] string name,
            [FromQuery] string? assemblyGuid)
        {
            var packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);
            var result = _installationManager.FilterPackages(
                packages,
                name,
                string.IsNullOrEmpty(assemblyGuid) ? default : Guid.Parse(assemblyGuid)).FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Gets available packages.
        /// </summary>
        /// <returns>Packages information.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PackageInfo[]), StatusCodes.Status200OK)]
        public async Task<IEnumerable<PackageInfo>> GetPackages()
        {
            IEnumerable<PackageInfo> packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);

            return packages;
        }

        /// <summary>
        /// Installs a package.
        /// </summary>
        /// <param name="name">Package name.</param>
        /// <param name="assemblyGuid">Guid of the associated assembly.</param>
        /// <param name="version">Optional version. Defaults to latest version.</param>
        /// <returns>Status.</returns>
        [HttpPost("/Installed/{Name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = Policies.RequiresElevation)]
        public async Task<ActionResult> InstallPackage(
            [FromRoute] [Required] string name,
            [FromQuery] string assemblyGuid,
            [FromQuery] string version)
        {
            var packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);
            var package = _installationManager.GetCompatibleVersions(
                    packages,
                    name,
                    string.IsNullOrEmpty(assemblyGuid) ? Guid.Empty : Guid.Parse(assemblyGuid),
                    string.IsNullOrEmpty(version) ? null : Version.Parse(version)).FirstOrDefault();

            if (package == null)
            {
                return NotFound();
            }

            await _installationManager.InstallPackage(package).ConfigureAwait(false);

            return Ok();
        }

        /// <summary>
        /// Cancels a package installation.
        /// </summary>
        /// <param name="id">Installation Id.</param>
        /// <returns>Status.</returns>
        [HttpDelete("/Installing/{id}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        public IActionResult CancelPackageInstallation(
            [FromRoute] [Required] string id)
        {
            _installationManager.CancelInstallation(new Guid(id));

            return Ok();
        }
    }
}
