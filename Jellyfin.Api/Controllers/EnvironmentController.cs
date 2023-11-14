using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.EnvironmentDtos;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Environment Controller.
/// </summary>
[Authorize(Policy = Policies.FirstTimeSetupOrElevated)]
public class EnvironmentController : BaseJellyfinApiController
{
    private const char UncSeparator = '\\';
    private const string UncStartPrefix = @"\\";

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<EnvironmentController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentController"/> class.
    /// </summary>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{EnvironmentController}"/> interface.</param>
    public EnvironmentController(IFileSystem fileSystem, ILogger<EnvironmentController> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Gets the contents of a given directory in the file system.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="includeFiles">An optional filter to include or exclude files from the results. true/false.</param>
    /// <param name="includeDirectories">An optional filter to include or exclude folders from the results. true/false.</param>
    /// <response code="200">Directory contents returned.</response>
    /// <returns>Directory contents.</returns>
    [HttpGet("DirectoryContents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<FileSystemEntryInfo> GetDirectoryContents(
        [FromQuery, Required] string path,
        [FromQuery] bool includeFiles = false,
        [FromQuery] bool includeDirectories = false)
    {
        if (path.StartsWith(UncStartPrefix, StringComparison.OrdinalIgnoreCase)
            && path.LastIndexOf(UncSeparator) == 1)
        {
            return Array.Empty<FileSystemEntryInfo>();
        }

        var entries =
            _fileSystem.GetFileSystemEntries(path)
                .Where(i => (i.IsDirectory && includeDirectories) || (!i.IsDirectory && includeFiles))
                .OrderBy(i => i.FullName);

        return entries.Select(f => new FileSystemEntryInfo(f.Name, f.FullName, f.IsDirectory ? FileSystemEntryType.Directory : FileSystemEntryType.File));
    }

    /// <summary>
    /// Validates path.
    /// </summary>
    /// <param name="validatePathDto">Validate request object.</param>
    /// <response code="204">Path validated.</response>
    /// <response code="404">Path not found.</response>
    /// <returns>Validation status.</returns>
    [HttpPost("ValidatePath")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult ValidatePath([FromBody, Required] ValidatePathDto validatePathDto)
    {
        if (validatePathDto.IsFile.HasValue)
        {
            if (validatePathDto.IsFile.Value)
            {
                if (!System.IO.File.Exists(validatePathDto.Path))
                {
                    return NotFound();
                }
            }
            else
            {
                if (!Directory.Exists(validatePathDto.Path))
                {
                    return NotFound();
                }
            }
        }
        else
        {
            if (!System.IO.File.Exists(validatePathDto.Path) && !Directory.Exists(validatePathDto.Path))
            {
                return NotFound();
            }

            if (validatePathDto.ValidateWritable)
            {
                if (validatePathDto.Path is null)
                {
                    throw new ResourceNotFoundException(nameof(validatePathDto.Path));
                }

                var file = Path.Combine(validatePathDto.Path, Guid.NewGuid().ToString());
                try
                {
                    System.IO.File.WriteAllText(file, string.Empty);
                }
                finally
                {
                    if (System.IO.File.Exists(file))
                    {
                        System.IO.File.Delete(file);
                    }
                }
            }
        }

        return NoContent();
    }

    /// <summary>
    /// Gets network paths.
    /// </summary>
    /// <response code="200">Empty array returned.</response>
    /// <returns>List of entries.</returns>
    [Obsolete("This endpoint is obsolete.")]
    [HttpGet("NetworkShares")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<FileSystemEntryInfo>> GetNetworkShares()
    {
        _logger.LogWarning("Obsolete endpoint accessed: /Environment/NetworkShares");
        return Array.Empty<FileSystemEntryInfo>();
    }

    /// <summary>
    /// Gets available drives from the server's file system.
    /// </summary>
    /// <response code="200">List of entries returned.</response>
    /// <returns>List of entries.</returns>
    [HttpGet("Drives")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<FileSystemEntryInfo> GetDrives()
    {
        return _fileSystem.GetDrives().Select(d => new FileSystemEntryInfo(d.Name, d.FullName, FileSystemEntryType.Directory));
    }

    /// <summary>
    /// Gets the parent path of a given path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>Parent path.</returns>
    [HttpGet("ParentPath")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<string?> GetParentPath([FromQuery, Required] string path)
    {
        string? parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
        {
            // Check if unc share
            var index = path.LastIndexOf(UncSeparator);

            if (index != -1 && path[0] == UncSeparator)
            {
                parent = path.Substring(0, index);

                if (string.IsNullOrWhiteSpace(parent.TrimStart(UncSeparator)))
                {
                    parent = null;
                }
            }
        }

        return parent;
    }

    /// <summary>
    /// Get Default directory browser.
    /// </summary>
    /// <response code="200">Default directory browser returned.</response>
    /// <returns>Default directory browser.</returns>
    [HttpGet("DefaultDirectoryBrowser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<DefaultDirectoryBrowserInfoDto> GetDefaultDirectoryBrowser()
    {
        return new DefaultDirectoryBrowserInfoDto();
    }
}
