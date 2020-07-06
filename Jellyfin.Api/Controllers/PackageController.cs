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
    [Authorize(Policy = Policies.DefaultAuthorization)]
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
        /// Gets a package by name or assembly GUID.
        /// </summary>
        /// <param name="name">The name of the package.</param>
        /// <param name="assemblyGuid">The GUID of the associated assembly.</param>
        /// <response code="200">Package retrieved.</response>
        /// <returns>A <see cref="PackageInfo"/> containing package information.</returns>
        [HttpGet("/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PackageInfo>> GetPackageInfo(
            [FromRoute] [Required] string? name,
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
        /// <response code="200">Available packages returned.</response>
        /// <returns>An <see cref="PackageInfo"/> containing available packages information.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IEnumerable<PackageInfo>> GetPackages()
        {
            IEnumerable<PackageInfo> packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);

            return packages;
        }

        /// <summary>
        /// Installs a package.
        /// </summary>
        /// <param name="name">Package name.</param>
        /// <param name="assemblyGuid">GUID of the associated assembly.</param>
        /// <param name="version">Optional version. Defaults to latest version.</param>
        /// <response code="204">Package found.</response>
        /// <response code="404">Package not found.</response>
        /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the package could not be found.</returns>
        [HttpPost("/Installed/{name}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = Policies.RequiresElevation)]
        public async Task<ActionResult> InstallPackage(
            [FromRoute] [Required] string? name,
            [FromQuery] string? assemblyGuid,
            [FromQuery] string? version)
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

            return NoContent();
        }

        /// <summary>
        /// Cancels a package installation.
        /// </summary>
        /// <param name="packageId">Installation Id.</param>
        /// <response code="204">Installation cancelled.</response>
        /// <returns>A <see cref="NoContentResult"/> on successfully cancelling a package installation.</returns>
        [HttpDelete("/Installing/{packageId}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult CancelPackageInstallation(
            [FromRoute] [Required] Guid packageId)
        {
            _installationManager.CancelInstallation(packageId);
            return NoContent();
        }
    }
}
