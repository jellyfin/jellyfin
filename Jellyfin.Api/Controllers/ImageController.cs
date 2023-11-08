using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Image controller.
/// </summary>
[Route("")]
public class ImageController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IImageProcessor _imageProcessor;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ImageController> _logger;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IApplicationPaths _appPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ImageController}"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    public ImageController(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        IImageProcessor imageProcessor,
        IFileSystem fileSystem,
        ILogger<ImageController> logger,
        IServerConfigurationManager serverConfigurationManager,
        IApplicationPaths appPaths)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _imageProcessor = imageProcessor;
        _fileSystem = fileSystem;
        _logger = logger;
        _serverConfigurationManager = serverConfigurationManager;
        _appPaths = appPaths;
    }

    private static Stream GetFromBase64Stream(Stream inputStream)
        => new CryptoStream(inputStream, new FromBase64Transform(), CryptoStreamMode.Read);

    /// <summary>
    /// Sets the user image.
    /// </summary>
    /// <param name="userId">User Id.</param>
    /// <param name="imageType">(Unused) Image type.</param>
    /// <param name="index">(Unused) Image index.</param>
    /// <response code="204">Image updated.</response>
    /// <response code="403">User does not have permission to delete the image.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Users/{userId}/Images/{imageType}")]
    [Authorize]
    [AcceptsImageFile]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageType", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> PostUserImage(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] int? index = null)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (!RequestHelpers.AssertCanUpdateUser(_userManager, HttpContext.User, userId, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to update the image.");
        }

        if (!TryGetImageExtensionFromContentType(Request.ContentType, out string? extension))
        {
            return BadRequest("Incorrect ContentType.");
        }

        var stream = GetFromBase64Stream(Request.Body);
        await using (stream.ConfigureAwait(false))
        {
            // Handle image/png; charset=utf-8
            var mimeType = Request.ContentType?.Split(';').FirstOrDefault();
            var userDataPath = Path.Combine(_serverConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath, user.Username);
            if (user.ProfileImage is not null)
            {
                await _userManager.ClearProfileImageAsync(user).ConfigureAwait(false);
            }

            user.ProfileImage = new Data.Entities.ImageInfo(Path.Combine(userDataPath, "profile" + extension));

            await _providerManager
                .SaveImage(stream, mimeType, user.ProfileImage.Path)
                .ConfigureAwait(false);
            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

            return NoContent();
        }
    }

    /// <summary>
    /// Sets the user image.
    /// </summary>
    /// <param name="userId">User Id.</param>
    /// <param name="imageType">(Unused) Image type.</param>
    /// <param name="index">(Unused) Image index.</param>
    /// <response code="204">Image updated.</response>
    /// <response code="403">User does not have permission to delete the image.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Users/{userId}/Images/{imageType}/{index}")]
    [Authorize]
    [AcceptsImageFile]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageType", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> PostUserImageByIndex(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute] int index)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (!RequestHelpers.AssertCanUpdateUser(_userManager, HttpContext.User, userId, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to update the image.");
        }

        if (!TryGetImageExtensionFromContentType(Request.ContentType, out string? extension))
        {
            return BadRequest("Incorrect ContentType.");
        }

        var stream = GetFromBase64Stream(Request.Body);
        await using (stream.ConfigureAwait(false))
        {
            // Handle image/png; charset=utf-8
            var mimeType = Request.ContentType?.Split(';').FirstOrDefault();
            var userDataPath = Path.Combine(_serverConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath, user.Username);
            if (user.ProfileImage is not null)
            {
                await _userManager.ClearProfileImageAsync(user).ConfigureAwait(false);
            }

            user.ProfileImage = new Data.Entities.ImageInfo(Path.Combine(userDataPath, "profile" + extension));

            await _providerManager
                .SaveImage(stream, mimeType, user.ProfileImage.Path)
                .ConfigureAwait(false);
            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

            return NoContent();
        }
    }

    /// <summary>
    /// Delete the user's image.
    /// </summary>
    /// <param name="userId">User Id.</param>
    /// <param name="imageType">(Unused) Image type.</param>
    /// <param name="index">(Unused) Image index.</param>
    /// <response code="204">Image deleted.</response>
    /// <response code="403">User does not have permission to delete the image.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("Users/{userId}/Images/{imageType}")]
    [Authorize]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageType", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteUserImage(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] int? index = null)
    {
        if (!RequestHelpers.AssertCanUpdateUser(_userManager, HttpContext.User, userId, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to delete the image.");
        }

        var user = _userManager.GetUserById(userId);
        if (user?.ProfileImage is null)
        {
            return NoContent();
        }

        try
        {
            System.IO.File.Delete(user.ProfileImage.Path);
        }
        catch (IOException e)
        {
            _logger.LogError(e, "Error deleting user profile image:");
        }

        await _userManager.ClearProfileImageAsync(user).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Delete the user's image.
    /// </summary>
    /// <param name="userId">User Id.</param>
    /// <param name="imageType">(Unused) Image type.</param>
    /// <param name="index">(Unused) Image index.</param>
    /// <response code="204">Image deleted.</response>
    /// <response code="403">User does not have permission to delete the image.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("Users/{userId}/Images/{imageType}/{index}")]
    [Authorize]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageType", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteUserImageByIndex(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute] int index)
    {
        if (!RequestHelpers.AssertCanUpdateUser(_userManager, HttpContext.User, userId, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to delete the image.");
        }

        var user = _userManager.GetUserById(userId);
        if (user?.ProfileImage is null)
        {
            return NoContent();
        }

        try
        {
            System.IO.File.Delete(user.ProfileImage.Path);
        }
        catch (IOException e)
        {
            _logger.LogError(e, "Error deleting user profile image:");
        }

        await _userManager.ClearProfileImageAsync(user).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Delete an item's image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">The image index.</param>
    /// <response code="204">Image deleted.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
    [HttpDelete("Items/{itemId}/Images/{imageType}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteItemImage(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] int? imageIndex)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        await item.DeleteImageAsync(imageType, imageIndex ?? 0).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Delete an item's image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">The image index.</param>
    /// <response code="204">Image deleted.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
    [HttpDelete("Items/{itemId}/Images/{imageType}/{imageIndex}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteItemImageByIndex(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute] int imageIndex)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        await item.DeleteImageAsync(imageType, imageIndex).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Set item image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <response code="204">Image saved.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
    [HttpPost("Items/{itemId}/Images/{imageType}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [AcceptsImageFile]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> SetItemImage(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (!TryGetImageExtensionFromContentType(Request.ContentType, out _))
        {
            return BadRequest("Incorrect ContentType.");
        }

        var stream = GetFromBase64Stream(Request.Body);
        await using (stream.ConfigureAwait(false))
        {
            // Handle image/png; charset=utf-8
            var mimeType = Request.ContentType?.Split(';').FirstOrDefault();
            await _providerManager.SaveImage(item, stream, mimeType, imageType, null, CancellationToken.None).ConfigureAwait(false);
            await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);

            return NoContent();
        }
    }

    /// <summary>
    /// Set item image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">(Unused) Image index.</param>
    /// <response code="204">Image saved.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
    [HttpPost("Items/{itemId}/Images/{imageType}/{imageIndex}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [AcceptsImageFile]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> SetItemImageByIndex(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute] int imageIndex)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (!TryGetImageExtensionFromContentType(Request.ContentType, out _))
        {
            return BadRequest("Incorrect ContentType.");
        }

        var stream = GetFromBase64Stream(Request.Body);
        await using (stream.ConfigureAwait(false))
        {
            // Handle image/png; charset=utf-8
            var mimeType = Request.ContentType?.Split(';').FirstOrDefault();
            await _providerManager.SaveImage(item, stream, mimeType, imageType, null, CancellationToken.None).ConfigureAwait(false);
            await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);

            return NoContent();
        }
    }

    /// <summary>
    /// Updates the index for an item image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">Old image index.</param>
    /// <param name="newIndex">New image index.</param>
    /// <response code="204">Image index updated.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
    [HttpPost("Items/{itemId}/Images/{imageType}/{imageIndex}/Index")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateItemImageIndex(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute, Required] int imageIndex,
        [FromQuery, Required] int newIndex)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        await item.SwapImagesAsync(imageType, imageIndex, newIndex).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Get item image infos.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <response code="200">Item images returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>The list of image infos on success, or <see cref="NotFoundResult"/> if item not found.</returns>
    [HttpGet("Items/{itemId}/Images")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ImageInfo>>> GetItemImageInfos([FromRoute, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        var list = new List<ImageInfo>();
        var itemImages = item.ImageInfos;

        if (itemImages.Length == 0)
        {
            // short-circuit
            return list;
        }

        await _libraryManager.UpdateImagesAsync(item).ConfigureAwait(false); // this makes sure dimensions and hashes are correct

        foreach (var image in itemImages)
        {
            if (!item.AllowsMultipleImages(image.Type))
            {
                var info = GetImageInfo(item, image, null);

                if (info is not null)
                {
                    list.Add(info);
                }
            }
        }

        foreach (var imageType in itemImages.Select(i => i.Type).Distinct().Where(item.AllowsMultipleImages))
        {
            var index = 0;

            // Prevent implicitly captured closure
            var currentImageType = imageType;

            foreach (var image in itemImages.Where(i => i.Type == currentImageType))
            {
                var info = GetImageInfo(item, image, index);

                if (info is not null)
                {
                    list.Add(info);
                }

                index++;
            }
        }

        return list;
    }

    /// <summary>
    /// Gets the item's image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="format">Optional. The <see cref="ImageFormat"/> of the returned image.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Items/{itemId}/Images/{imageType}")]
    [HttpHead("Items/{itemId}/Images/{imageType}", Name = "HeadItemImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetItemImage(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery] string? tag,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] ImageFormat? format,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromQuery] int? imageIndex)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                itemId,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the item's image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="format">Optional. The <see cref="ImageFormat"/> of the returned image.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Items/{itemId}/Images/{imageType}/{imageIndex}")]
    [HttpHead("Items/{itemId}/Images/{imageType}/{imageIndex}", Name = "HeadItemImageByIndex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetItemImageByIndex(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute] int imageIndex,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery] string? tag,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] ImageFormat? format,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                itemId,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the item's image.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Items/{itemId}/Images/{imageType}/{imageIndex}/{tag}/{format}/{maxWidth}/{maxHeight}/{percentPlayed}/{unplayedCount}")]
    [HttpHead("Items/{itemId}/Images/{imageType}/{imageIndex}/{tag}/{format}/{maxWidth}/{maxHeight}/{percentPlayed}/{unplayedCount}", Name = "HeadItemImage2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetItemImage2(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute, Required] int maxWidth,
        [FromRoute, Required] int maxHeight,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromRoute, Required] string tag,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromRoute, Required] ImageFormat format,
        [FromRoute, Required] double percentPlayed,
        [FromRoute, Required] int unplayedCount,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromRoute, Required] int imageIndex)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                itemId,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get artist image by name.
    /// </summary>
    /// <param name="name">Artist name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Artists/{name}/Images/{imageType}/{imageIndex}")]
    [HttpHead("Artists/{name}/Images/{imageType}/{imageIndex}", Name = "HeadArtistImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetArtistImage(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromRoute, Required] int imageIndex)
    {
        var item = _libraryManager.GetArtist(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get genre image by name.
    /// </summary>
    /// <param name="name">Genre name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Genres/{name}/Images/{imageType}")]
    [HttpHead("Genres/{name}/Images/{imageType}", Name = "HeadGenreImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetGenreImage(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromQuery] int? imageIndex)
    {
        var item = _libraryManager.GetGenre(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get genre image by name.
    /// </summary>
    /// <param name="name">Genre name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Genres/{name}/Images/{imageType}/{imageIndex}")]
    [HttpHead("Genres/{name}/Images/{imageType}/{imageIndex}", Name = "HeadGenreImageByIndex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetGenreImageByIndex(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromRoute, Required] int imageIndex,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer)
    {
        var item = _libraryManager.GetGenre(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get music genre image by name.
    /// </summary>
    /// <param name="name">Music genre name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("MusicGenres/{name}/Images/{imageType}")]
    [HttpHead("MusicGenres/{name}/Images/{imageType}", Name = "HeadMusicGenreImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetMusicGenreImage(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromQuery] int? imageIndex)
    {
        var item = _libraryManager.GetMusicGenre(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get music genre image by name.
    /// </summary>
    /// <param name="name">Music genre name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("MusicGenres/{name}/Images/{imageType}/{imageIndex}")]
    [HttpHead("MusicGenres/{name}/Images/{imageType}/{imageIndex}", Name = "HeadMusicGenreImageByIndex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetMusicGenreImageByIndex(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromRoute, Required] int imageIndex,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer)
    {
        var item = _libraryManager.GetMusicGenre(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get person image by name.
    /// </summary>
    /// <param name="name">Person name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Persons/{name}/Images/{imageType}")]
    [HttpHead("Persons/{name}/Images/{imageType}", Name = "HeadPersonImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetPersonImage(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromQuery] int? imageIndex)
    {
        var item = _libraryManager.GetPerson(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get person image by name.
    /// </summary>
    /// <param name="name">Person name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Persons/{name}/Images/{imageType}/{imageIndex}")]
    [HttpHead("Persons/{name}/Images/{imageType}/{imageIndex}", Name = "HeadPersonImageByIndex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetPersonImageByIndex(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromRoute, Required] int imageIndex,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer)
    {
        var item = _libraryManager.GetPerson(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get studio image by name.
    /// </summary>
    /// <param name="name">Studio name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Studios/{name}/Images/{imageType}")]
    [HttpHead("Studios/{name}/Images/{imageType}", Name = "HeadStudioImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetStudioImage(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromQuery] int? imageIndex)
    {
        var item = _libraryManager.GetStudio(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get studio image by name.
    /// </summary>
    /// <param name="name">Studio name.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Studios/{name}/Images/{imageType}/{imageIndex}")]
    [HttpHead("Studios/{name}/Images/{imageType}/{imageIndex}", Name = "HeadStudioImageByIndex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetStudioImageByIndex(
        [FromRoute, Required] string name,
        [FromRoute, Required] ImageType imageType,
        [FromRoute, Required] int imageIndex,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer)
    {
        var item = _libraryManager.GetStudio(name);
        if (item is null)
        {
            return NotFound();
        }

        return await GetImageInternal(
                item.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                item)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get user profile image.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Users/{userId}/Images/{imageType}")]
    [HttpHead("Users/{userId}/Images/{imageType}", Name = "HeadUserImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetUserImage(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] ImageType imageType,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromQuery] int? imageIndex)
    {
        var user = _userManager.GetUserById(userId);
        if (user?.ProfileImage is null)
        {
            return NotFound();
        }

        var info = new ItemImageInfo
        {
            Path = user.ProfileImage.Path,
            Type = ImageType.Profile,
            DateModified = user.ProfileImage.LastModified
        };

        if (width.HasValue)
        {
            info.Width = width.Value;
        }

        if (height.HasValue)
        {
            info.Height = height.Value;
        }

        return await GetImageInternal(
                user.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                null,
                info)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get user profile image.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="imageType">Image type.</param>
    /// <param name="imageIndex">Image index.</param>
    /// <param name="tag">Optional. Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="percentPlayed">Optional. Percent to render for the percent played overlay.</param>
    /// <param name="unplayedCount">Optional. Unplayed count overlay to render.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="quality">Optional. Quality setting, from 0-100. Defaults to 90 and should suffice in most cases.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="cropWhitespace">Optional. Specify if whitespace should be cropped out of the image. True/False. If unspecified, whitespace will be cropped from logos and clear art.</param>
    /// <param name="blur">Optional. Blur image.</param>
    /// <param name="backgroundColor">Optional. Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Optional. Apply a foreground layer on top of the image.</param>
    /// <response code="200">Image stream returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>
    /// A <see cref="FileStreamResult"/> containing the file stream on success,
    /// or a <see cref="NotFoundResult"/> if item not found.
    /// </returns>
    [HttpGet("Users/{userId}/Images/{imageType}/{imageIndex}")]
    [HttpHead("Users/{userId}/Images/{imageType}/{imageIndex}", Name = "HeadUserImageByIndex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public async Task<ActionResult> GetUserImageByIndex(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] ImageType imageType,
        [FromRoute, Required] int imageIndex,
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] double? percentPlayed,
        [FromQuery] int? unplayedCount,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? quality,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery, ParameterObsolete] bool? cropWhitespace,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer)
    {
        var user = _userManager.GetUserById(userId);
        if (user?.ProfileImage is null)
        {
            return NotFound();
        }

        var info = new ItemImageInfo
        {
            Path = user.ProfileImage.Path,
            Type = ImageType.Profile,
            DateModified = user.ProfileImage.LastModified
        };

        if (width.HasValue)
        {
            info.Width = width.Value;
        }

        if (height.HasValue)
        {
            info.Height = height.Value;
        }

        return await GetImageInternal(
                user.Id,
                imageType,
                imageIndex,
                tag,
                format,
                maxWidth,
                maxHeight,
                percentPlayed,
                unplayedCount,
                width,
                height,
                quality,
                fillWidth,
                fillHeight,
                blur,
                backgroundColor,
                foregroundLayer,
                null,
                info)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Generates or gets the splashscreen.
    /// </summary>
    /// <param name="tag">Supply the cache tag from the item object to receive strong caching headers.</param>
    /// <param name="format">Determines the output format of the image - original,gif,jpg,png.</param>
    /// <param name="maxWidth">The maximum image width to return.</param>
    /// <param name="maxHeight">The maximum image height to return.</param>
    /// <param name="width">The fixed image width to return.</param>
    /// <param name="height">The fixed image height to return.</param>
    /// <param name="fillWidth">Width of box to fill.</param>
    /// <param name="fillHeight">Height of box to fill.</param>
    /// <param name="blur">Blur image.</param>
    /// <param name="backgroundColor">Apply a background color for transparent images.</param>
    /// <param name="foregroundLayer">Apply a foreground layer on top of the image.</param>
    /// <param name="quality">Quality setting, from 0-100.</param>
    /// <response code="200">Splashscreen returned successfully.</response>
    /// <returns>The splashscreen.</returns>
    [HttpGet("Branding/Splashscreen")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesImageFile]
    public async Task<ActionResult> GetSplashscreen(
        [FromQuery] string? tag,
        [FromQuery] ImageFormat? format,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? fillWidth,
        [FromQuery] int? fillHeight,
        [FromQuery] int? blur,
        [FromQuery] string? backgroundColor,
        [FromQuery] string? foregroundLayer,
        [FromQuery, Range(0, 100)] int quality = 90)
    {
        var brandingOptions = _serverConfigurationManager.GetConfiguration<BrandingOptions>("branding");
        if (!brandingOptions.SplashscreenEnabled)
        {
            return NotFound();
        }

        string splashscreenPath;

        if (!string.IsNullOrWhiteSpace(brandingOptions.SplashscreenLocation)
            && System.IO.File.Exists(brandingOptions.SplashscreenLocation))
        {
            splashscreenPath = brandingOptions.SplashscreenLocation;
        }
        else
        {
            splashscreenPath = Path.Combine(_appPaths.DataPath, "splashscreen.png");
            if (!System.IO.File.Exists(splashscreenPath))
            {
                return NotFound();
            }
        }

        var outputFormats = GetOutputFormats(format);

        TimeSpan? cacheDuration = null;
        if (!string.IsNullOrEmpty(tag))
        {
            cacheDuration = TimeSpan.FromDays(365);
        }

        var options = new ImageProcessingOptions
        {
            Image = new ItemImageInfo
            {
                Path = splashscreenPath
            },
            Height = height,
            MaxHeight = maxHeight,
            MaxWidth = maxWidth,
            FillHeight = fillHeight,
            FillWidth = fillWidth,
            Quality = quality,
            Width = width,
            Blur = blur,
            BackgroundColor = backgroundColor,
            ForegroundLayer = foregroundLayer,
            SupportedOutputFormats = outputFormats
        };

        return await GetImageResult(
                options,
                cacheDuration,
                ImmutableDictionary<string, string>.Empty)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Uploads a custom splashscreen.
    /// The body is expected to the image contents base64 encoded.
    /// </summary>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    /// <response code="204">Successfully uploaded new splashscreen.</response>
    /// <response code="400">Error reading MimeType from uploaded image.</response>
    /// <response code="403">User does not have permission to upload splashscreen..</response>
    /// <exception cref="ArgumentException">Error reading the image format.</exception>
    [HttpPost("Branding/Splashscreen")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [AcceptsImageFile]
    public async Task<ActionResult> UploadCustomSplashscreen()
    {
        if (!TryGetImageExtensionFromContentType(Request.ContentType, out var extension))
        {
            return BadRequest("Incorrect ContentType.");
        }

        var stream = GetFromBase64Stream(Request.Body);
        await using (stream.ConfigureAwait(false))
        {
            var filePath = Path.Combine(_appPaths.DataPath, "splashscreen-upload" + extension);
            var brandingOptions = _serverConfigurationManager.GetConfiguration<BrandingOptions>("branding");
            brandingOptions.SplashscreenLocation = filePath;
            _serverConfigurationManager.SaveConfiguration("branding", brandingOptions);

            var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
            await using (fs.ConfigureAwait(false))
            {
                await stream.CopyToAsync(fs, CancellationToken.None).ConfigureAwait(false);
            }

            return NoContent();
        }
    }

    /// <summary>
    /// Delete a custom splashscreen.
    /// </summary>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    /// <response code="204">Successfully deleted the custom splashscreen.</response>
    /// <response code="403">User does not have permission to delete splashscreen..</response>
    [HttpDelete("Branding/Splashscreen")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult DeleteCustomSplashscreen()
    {
        var brandingOptions = _serverConfigurationManager.GetConfiguration<BrandingOptions>("branding");
        if (!string.IsNullOrEmpty(brandingOptions.SplashscreenLocation)
            && System.IO.File.Exists(brandingOptions.SplashscreenLocation))
        {
            System.IO.File.Delete(brandingOptions.SplashscreenLocation);
            brandingOptions.SplashscreenLocation = null;
            _serverConfigurationManager.SaveConfiguration("branding", brandingOptions);
        }

        return NoContent();
    }

    private ImageInfo? GetImageInfo(BaseItem item, ItemImageInfo info, int? imageIndex)
    {
        int? width = null;
        int? height = null;
        string? blurhash = null;
        long length = 0;

        try
        {
            if (info.IsLocalFile)
            {
                var fileInfo = _fileSystem.GetFileInfo(info.Path);
                length = fileInfo.Length;

                blurhash = info.BlurHash;
                width = info.Width;
                height = info.Height;

                if (width <= 0 || height <= 0)
                {
                    width = null;
                    height = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image information for {Item}", item.Name);
        }

        try
        {
            return new ImageInfo
            {
                Path = info.Path,
                ImageIndex = imageIndex,
                ImageType = info.Type,
                ImageTag = _imageProcessor.GetImageCacheTag(item, info),
                Size = length,
                BlurHash = blurhash,
                Width = width,
                Height = height
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image information for {Path}", info.Path);
            return null;
        }
    }

    private async Task<ActionResult> GetImageInternal(
        Guid itemId,
        ImageType imageType,
        int? imageIndex,
        string? tag,
        ImageFormat? format,
        int? maxWidth,
        int? maxHeight,
        double? percentPlayed,
        int? unplayedCount,
        int? width,
        int? height,
        int? quality,
        int? fillWidth,
        int? fillHeight,
        int? blur,
        string? backgroundColor,
        string? foregroundLayer,
        BaseItem? item,
        ItemImageInfo? imageInfo = null)
    {
        if (percentPlayed.HasValue)
        {
            if (percentPlayed.Value <= 0)
            {
                percentPlayed = null;
            }
            else if (percentPlayed.Value >= 100)
            {
                percentPlayed = null;
            }
        }

        if (percentPlayed.HasValue)
        {
            unplayedCount = null;
        }

        if (unplayedCount.HasValue
            && unplayedCount.Value <= 0)
        {
            unplayedCount = null;
        }

        if (imageInfo is null)
        {
            imageInfo = item?.GetImageInfo(imageType, imageIndex ?? 0);
            if (imageInfo is null)
            {
                return NotFound(string.Format(NumberFormatInfo.InvariantInfo, "{0} does not have an image of type {1}", item?.Name, imageType));
            }
        }

        var outputFormats = GetOutputFormats(format);

        TimeSpan? cacheDuration = null;

        if (!string.IsNullOrEmpty(tag))
        {
            cacheDuration = TimeSpan.FromDays(365);
        }

        var responseHeaders = new Dictionary<string, string>
        {
            { "transferMode.dlna.org", "Interactive" },
            { "realTimeInfo.dlna.org", "DLNA.ORG_TLAG=*" }
        };

        if (!imageInfo.IsLocalFile && item is not null)
        {
            imageInfo = await _libraryManager.ConvertImageToLocal(item, imageInfo, imageIndex ?? 0).ConfigureAwait(false);
        }

        var options = new ImageProcessingOptions
        {
            Height = height,
            ImageIndex = imageIndex ?? 0,
            Image = imageInfo,
            Item = item,
            ItemId = itemId,
            MaxHeight = maxHeight,
            MaxWidth = maxWidth,
            FillHeight = fillHeight,
            FillWidth = fillWidth,
            Quality = quality ?? 100,
            Width = width,
            PercentPlayed = percentPlayed ?? 0,
            UnplayedCount = unplayedCount,
            Blur = blur,
            BackgroundColor = backgroundColor,
            ForegroundLayer = foregroundLayer,
            SupportedOutputFormats = outputFormats
        };

        return await GetImageResult(
            options,
            cacheDuration,
            responseHeaders).ConfigureAwait(false);
    }

    private ImageFormat[] GetOutputFormats(ImageFormat? format)
    {
        if (format.HasValue)
        {
            return new[] { format.Value };
        }

        return GetClientSupportedFormats();
    }

    private ImageFormat[] GetClientSupportedFormats()
    {
        var supportedFormats = Request.Headers.GetCommaSeparatedValues(HeaderNames.Accept);
        for (var i = 0; i < supportedFormats.Length; i++)
        {
            // Remove charsets etc. (anything after semi-colon)
            var type = supportedFormats[i];
            int index = type.IndexOf(';', StringComparison.Ordinal);
            if (index != -1)
            {
                supportedFormats[i] = type.Substring(0, index);
            }
        }

        var acceptParam = Request.Query[HeaderNames.Accept];

        var supportsWebP = SupportsFormat(supportedFormats, acceptParam, ImageFormat.Webp, false);

        if (!supportsWebP)
        {
            var userAgent = Request.Headers[HeaderNames.UserAgent].ToString();
            if (userAgent.Contains("crosswalk", StringComparison.OrdinalIgnoreCase)
                && userAgent.Contains("android", StringComparison.OrdinalIgnoreCase))
            {
                supportsWebP = true;
            }
        }

        var formats = new List<ImageFormat>(4);

        if (supportsWebP)
        {
            formats.Add(ImageFormat.Webp);
        }

        formats.Add(ImageFormat.Jpg);
        formats.Add(ImageFormat.Png);

        if (SupportsFormat(supportedFormats, acceptParam, ImageFormat.Gif, true))
        {
            formats.Add(ImageFormat.Gif);
        }

        return formats.ToArray();
    }

    private bool SupportsFormat(IReadOnlyCollection<string> requestAcceptTypes, string? acceptParam, ImageFormat format, bool acceptAll)
    {
        if (requestAcceptTypes.Contains(format.GetMimeType()))
        {
            return true;
        }

        if (acceptAll && requestAcceptTypes.Contains("*/*"))
        {
            return true;
        }

        // Review if this should be jpeg, jpg or both for ImageFormat.Jpg
        var normalized = format.ToString().ToLowerInvariant();
        return string.Equals(acceptParam, normalized, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ActionResult> GetImageResult(
        ImageProcessingOptions imageProcessingOptions,
        TimeSpan? cacheDuration,
        IDictionary<string, string> headers)
    {
        var (imagePath, imageContentType, dateImageModified) = await _imageProcessor.ProcessImage(imageProcessingOptions).ConfigureAwait(false);

        var disableCaching = Request.Headers[HeaderNames.CacheControl].Contains("no-cache");
        var parsingSuccessful = DateTime.TryParse(Request.Headers[HeaderNames.IfModifiedSince], out var ifModifiedSinceHeader);

        // if the parsing of the IfModifiedSince header was not successful, disable caching
        if (!parsingSuccessful)
        {
            // disableCaching = true;
        }

        foreach (var (key, value) in headers)
        {
            Response.Headers.Add(key, value);
        }

        Response.ContentType = imageContentType ?? MediaTypeNames.Text.Plain;
        Response.Headers.Add(HeaderNames.Age, Convert.ToInt64((DateTime.UtcNow - dateImageModified).TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Response.Headers.Add(HeaderNames.Vary, HeaderNames.Accept);

        if (disableCaching)
        {
            Response.Headers.Add(HeaderNames.CacheControl, "no-cache, no-store, must-revalidate");
            Response.Headers.Add(HeaderNames.Pragma, "no-cache, no-store, must-revalidate");
        }
        else
        {
            if (cacheDuration.HasValue)
            {
                Response.Headers.Add(HeaderNames.CacheControl, "public, max-age=" + cacheDuration.Value.TotalSeconds);
            }
            else
            {
                Response.Headers.Add(HeaderNames.CacheControl, "public");
            }

            Response.Headers.Add(HeaderNames.LastModified, dateImageModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss \"GMT\"", CultureInfo.InvariantCulture));

            // if the image was not modified since "ifModifiedSinceHeader"-header, return a HTTP status code 304 not modified
            if (!(dateImageModified > ifModifiedSinceHeader) && cacheDuration.HasValue)
            {
                if (ifModifiedSinceHeader.Add(cacheDuration.Value) < DateTime.UtcNow)
                {
                    Response.StatusCode = StatusCodes.Status304NotModified;
                    return new ContentResult();
                }
            }
        }

        return PhysicalFile(imagePath, imageContentType ?? MediaTypeNames.Text.Plain);
    }

    internal static bool TryGetImageExtensionFromContentType(string? contentType, [NotNullWhen(true)] out string? extension)
    {
        extension = null;
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        if (MediaTypeHeaderValue.TryParse(contentType, out var parsedValue)
            && parsedValue.MediaType.HasValue
            && MimeTypes.IsImage(parsedValue.MediaType.Value))
        {
            extension = MimeTypes.ToExtension(parsedValue.MediaType.Value);
            return extension is not null;
        }

        return false;
    }
}
