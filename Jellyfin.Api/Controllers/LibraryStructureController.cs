using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.LibraryStructureDto;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The library structure controller.
/// </summary>
[Route("Library/VirtualFolders")]
[Authorize(Policy = Policies.FirstTimeSetupOrElevated)]
public class LibraryStructureController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IVirtualFolderManager _virtualFolderManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryStructureController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="virtualFolderManager">Instance of <see cref="IVirtualFolderManager"/> interface.</param>
    public LibraryStructureController(ILibraryManager libraryManager, IVirtualFolderManager virtualFolderManager)
    {
        _libraryManager = libraryManager;
        _virtualFolderManager = virtualFolderManager;
    }

    /// <summary>
    /// Gets all virtual folders.
    /// </summary>
    /// <response code="200">Virtual folders retrieved.</response>
    /// <returns>An <see cref="IEnumerable{VirtualFolderInfo}"/> with the virtual folders.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<VirtualFolderInfo> GetVirtualFolders()
    {
        return _virtualFolderManager.GetVirtualFolders(true);
    }

    /// <summary>
    /// Adds a virtual folder.
    /// </summary>
    /// <param name="name">The name of the virtual folder.</param>
    /// <param name="collectionType">The type of the collection.</param>
    /// <param name="paths">The paths of the virtual folder.</param>
    /// <param name="libraryOptionsDto">The library options.</param>
    /// <param name="refreshLibrary">Whether to refresh the library.</param>
    /// <response code="204">Folder added.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> AddVirtualFolder(
        [FromQuery] string name,
        [FromQuery] CollectionTypeOptions? collectionType,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] paths,
        [FromBody] AddVirtualFolderDto? libraryOptionsDto,
        [FromQuery] bool refreshLibrary = false)
    {
        var libraryOptions = libraryOptionsDto?.LibraryOptions ?? new LibraryOptions();

        if (paths is not null && paths.Length > 0)
        {
            libraryOptions.PathInfos = paths.Select(i => new MediaPathInfo(i)).ToArray();
        }

        await _virtualFolderManager.AddVirtualFolder(name, collectionType, libraryOptions, refreshLibrary).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Removes a virtual folder.
    /// </summary>
    /// <param name="name">The name of the folder.</param>
    /// <param name="refreshLibrary">Whether to refresh the library.</param>
    /// <response code="204">Folder removed.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RemoveVirtualFolder(
        [FromQuery] string name,
        [FromQuery] bool refreshLibrary = false)
    {
        await _virtualFolderManager.RemoveVirtualFolder(name, refreshLibrary).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Renames a virtual folder.
    /// </summary>
    /// <param name="name">The name of the virtual folder.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="refreshLibrary">Whether to refresh the library.</param>
    /// <response code="204">Folder renamed.</response>
    /// <response code="404">Library doesn't exist.</response>
    /// <response code="409">Library already exists.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, a <see cref="NotFoundResult"/> if the library doesn't exist, a <see cref="ConflictResult"/> if the new name is already taken.</returns>
    /// <exception cref="ArgumentNullException">The new name may not be null.</exception>
    [HttpPost("Name")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RenameVirtualFolder(
        [FromQuery] string? name,
        [FromQuery] string? newName,
        [FromQuery] bool refreshLibrary = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentNullException(nameof(newName));
        }

        try
        {
            await _virtualFolderManager.RenameVirtualFolder(name, newName, refreshLibrary).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound("The media collection does not exist.");
        }
        catch (InvalidOperationException)
        {
            return Conflict("The media library already exists.");
        }

        return NoContent();
    }

    /// <summary>
    /// Add a media path to a library.
    /// </summary>
    /// <param name="mediaPathDto">The media path dto.</param>
    /// <param name="refreshLibrary">Whether to refresh the library.</param>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    /// <response code="204">Media path added.</response>
    /// <exception cref="ArgumentNullException">The name of the library may not be empty.</exception>
    [HttpPost("Paths")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult AddMediaPath(
        [FromBody, Required] MediaPathDto mediaPathDto,
        [FromQuery] bool refreshLibrary = false)
    {
        var mediaPath = mediaPathDto.PathInfo ?? new MediaPathInfo(mediaPathDto.Path ?? throw new ArgumentException("PathInfo and Path can't both be null."));

        _virtualFolderManager.AddMediaPath(mediaPathDto.Name, mediaPath, refreshLibrary);
        return NoContent();
    }

    /// <summary>
    /// Updates a media path.
    /// </summary>
    /// <param name="mediaPathRequestDto">The name of the library and path infos.</param>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    /// <response code="204">Media path updated.</response>
    /// <exception cref="ArgumentNullException">The name of the library may not be empty.</exception>
    [HttpPost("Paths/Update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateMediaPath([FromBody, Required] UpdateMediaPathRequestDto mediaPathRequestDto)
    {
        if (string.IsNullOrWhiteSpace(mediaPathRequestDto.Name))
        {
            throw new ArgumentNullException(nameof(mediaPathRequestDto), "Name must not be null or empty");
        }

        _virtualFolderManager.UpdateMediaPath(mediaPathRequestDto.Name, mediaPathRequestDto.PathInfo);
        return NoContent();
    }

    /// <summary>
    /// Remove a media path.
    /// </summary>
    /// <param name="name">The name of the library.</param>
    /// <param name="path">The path to remove.</param>
    /// <param name="refreshLibrary">Whether to refresh the library.</param>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    /// <response code="204">Media path removed.</response>
    /// <exception cref="ArgumentNullException">The name of the library may not be empty.</exception>
    [HttpDelete("Paths")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult RemoveMediaPath(
        [FromQuery] string name,
        [FromQuery] string path,
        [FromQuery] bool refreshLibrary = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        _virtualFolderManager.RemoveMediaPath(name, path, refreshLibrary);
        return NoContent();
    }

    /// <summary>
    /// Update library options.
    /// </summary>
    /// <param name="request">The library name and options.</param>
    /// <response code="204">Library updated.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("LibraryOptions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateLibraryOptions(
        [FromBody] UpdateLibraryOptionsDto request)
    {
        var collectionFolder = (CollectionFolder)_libraryManager.GetItemById(request.Id);

        collectionFolder.UpdateLibraryOptions(request.LibraryOptions);
        return NoContent();
    }
}
