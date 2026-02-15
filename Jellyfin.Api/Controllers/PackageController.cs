using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Package Controller.
/// </summary>
[Route("")]
[Authorize(Policy = Policies.RequiresElevation)]
public class PackageController : BaseJellyfinApiController
{
    private readonly IInstallationManager _installationManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageController"/> class.
    /// </summary>
    /// <param name="installationManager">Instance of the <see cref="IInstallationManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public PackageController(IInstallationManager installationManager, IServerConfigurationManager serverConfigurationManager)
    {
        _installationManager = installationManager;
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <summary>
    /// Gets a package by name or assembly GUID.
    /// </summary>
    /// <param name="name">The name of the package.</param>
    /// <param name="assemblyGuid">The GUID of the associated assembly.</param>
    /// <response code="200">Package retrieved.</response>
    /// <returns>A <see cref="PackageInfo"/> containing package information.</returns>
    [HttpGet("Packages/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PackageInfo>> GetPackageInfo(
        [FromRoute, Required] string name,
        [FromQuery] Guid? assemblyGuid)
    {
        var packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);
        var result = _installationManager.FilterPackages(
                packages,
                name,
                assemblyGuid ?? default)
            .FirstOrDefault();

        if (result is null)
        {
            return NotFound();
        }

        return result;
    }

    /// <summary>
    /// Gets available packages.
    /// </summary>
    /// <response code="200">Available packages returned.</response>
    /// <returns>An <see cref="PackageInfo"/> containing available packages information.</returns>
    [HttpGet("Packages")]
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
    /// <param name="repositoryUrl">Optional. Specify the repository to install from.</param>
    /// <response code="204">Package found.</response>
    /// <response code="404">Package not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the package could not be found.</returns>
    [HttpPost("Packages/Installed/{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> InstallPackage(
        [FromRoute, Required] string name,
        [FromQuery] Guid? assemblyGuid,
        [FromQuery] string? version,
        [FromQuery] string? repositoryUrl)
    {
        var packages = await _installationManager.GetAvailablePackages().ConfigureAwait(false);
        if (!string.IsNullOrEmpty(repositoryUrl))
        {
            packages = packages.Where(p => p.Versions.Any(q => q.RepositoryUrl.Equals(repositoryUrl, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var package = _installationManager.GetCompatibleVersions(
                packages,
                name,
                assemblyGuid ?? Guid.Empty,
                specificVersion: string.IsNullOrEmpty(version) ? null : Version.Parse(version))
            .FirstOrDefault();

        if (package is null)
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
    [HttpDelete("Packages/Installing/{packageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult CancelPackageInstallation(
        [FromRoute, Required] Guid packageId)
    {
        _installationManager.CancelInstallation(packageId);
        return NoContent();
    }

    /// <summary>
    /// Gets all package repositories.
    /// </summary>
    /// <response code="200">Package repositories returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of package repositories.</returns>
    [HttpGet("Repositories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<RepositoryInfo>> GetRepositories()
    {
        return Ok(_serverConfigurationManager.Configuration.PluginRepositories.AsEnumerable());
    }

    /// <summary>
    /// Sets the enabled and existing package repositories.
    /// </summary>
    /// <param name="repositoryInfos">The list of package repositories.</param>
    /// <response code="204">Package repositories saved.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Repositories")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult SetRepositories([FromBody, Required] RepositoryInfo[] repositoryInfos)
    {
        _serverConfigurationManager.Configuration.PluginRepositories = repositoryInfos;
        _serverConfigurationManager.SaveConfiguration();
        return NoContent();
    }
}
