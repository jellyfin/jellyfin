#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.EnvironmentDtos;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Environment Controller.
    /// </summary>
    [Authorize(Policy = Policies.RequiresElevation)]
    public class EnvironmentController : BaseJellyfinApiController
    {
        private const char UncSeparator = '\\';
        private const string UncSeparatorString = "\\";

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentController"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public EnvironmentController(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
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
            [FromQuery, BindRequired] string path,
            [FromQuery] bool includeFiles,
            [FromQuery] bool includeDirectories)
        {
            const string networkPrefix = UncSeparatorString + UncSeparatorString;
            if (path.StartsWith(networkPrefix, StringComparison.OrdinalIgnoreCase)
                && path.LastIndexOf(UncSeparator) == 1)
            {
                return Array.Empty<FileSystemEntryInfo>();
            }

            var entries = _fileSystem.GetFileSystemEntries(path).OrderBy(i => i.FullName).Where(i =>
            {
                var isDirectory = i.IsDirectory;

                if (!includeFiles && !isDirectory)
                {
                    return false;
                }

                return includeDirectories || !isDirectory;
            });

            return entries.Select(f => new FileSystemEntryInfo
            {
                Name = f.Name,
                Path = f.FullName,
                Type = f.IsDirectory ? FileSystemEntryType.Directory : FileSystemEntryType.File
            });
        }

        /// <summary>
        /// Validates path.
        /// </summary>
        /// <param name="validatePathDto">Validate request object.</param>
        /// <response code="200">Path validated.</response>
        /// <response code="404">Path not found.</response>
        /// <returns>Validation status.</returns>
        [HttpPost("ValidatePath")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult ValidatePath([FromBody, BindRequired] ValidatePathDto validatePathDto)
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

            return Ok();
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
            return _fileSystem.GetDrives().Select(d => new FileSystemEntryInfo
            {
                Name = d.Name,
                Path = d.FullName,
                Type = FileSystemEntryType.Directory
            });
        }

        /// <summary>
        /// Gets the parent path of a given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Parent path.</returns>
        [HttpGet("ParentPath")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<string?> GetParentPath([FromQuery, BindRequired] string path)
        {
            string? parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent))
            {
                // Check if unc share
                var index = path.LastIndexOf(UncSeparator);

                if (index != -1 && path.IndexOf(UncSeparator, StringComparison.OrdinalIgnoreCase) == 0)
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
        public ActionResult<DefaultDirectoryBrowserInfo> GetDefaultDirectoryBrowser()
        {
            return new DefaultDirectoryBrowserInfo();
        }
    }
}
