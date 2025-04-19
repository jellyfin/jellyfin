using System;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Backup;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The backup controller.
/// </summary>
[Route("Backup")]
[Authorize(Policy = Policies.RequiresElevation)]
public class BackupController : BaseJellyfinApiController
{
    private readonly IBackupService _backupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupController"/> class.
    /// </summary>
    /// <param name="backupService">Instance of the <see cref="IBackupService"/> interface.</param>
    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>
    /// Creates a new Backup.
    /// </summary>
    /// <response code="200">Backup created.</response>
    /// <response code="403">User does not have permission to retrieve information.</response>
    /// <returns>OK.</returns>
    [HttpPost("Create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BackupManifestDto>> CreateBackup()
    {
        return Ok(await _backupService.CreateBackupAsync().ConfigureAwait(false));
    }

    /// <summary>
    /// Restores to a backup by restarting the server and applying the backup.
    /// </summary>
    /// <param name="archivePath">The local path to the archive to restore from.</param>
    /// <response code="200">Backup restore started.</response>
    /// <response code="403">User does not have permission to retrieve information.</response>
    /// <returns>OK.</returns>
    [HttpPost("Restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult StartRestoreBackup(string archivePath)
    {
        if (!System.IO.File.Exists(archivePath))
        {
            return NotFound();
        }

        _backupService.ScheduleRestoreAndRestartServer(archivePath);
        return NoContent();
    }

    /// <summary>
    /// Gets a list of all currently present backups in the backup directory.
    /// </summary>
    /// <response code="200">Backups available.</response>
    /// <response code="403">User does not have permission to retrieve information.</response>
    /// <returns>OK.</returns>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BackupManifestDto[]>> GetBackups()
    {
        return Ok(await _backupService.EnumerateBackups().ConfigureAwait(false));
    }
}
