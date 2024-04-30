using System;
using System.Collections.Generic;
using System.Security.Claims;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Api.Extensions;

/// <summary>
/// Dto Extensions.
/// </summary>
public static class DtoExtensions
{
    /// <summary>
    /// Add additional fields depending on client.
    /// </summary>
    /// <remarks>
    /// Use in place of GetDtoOptions.
    /// Legacy order: 2.
    /// </remarks>
    /// <param name="dtoOptions">DtoOptions object.</param>
    /// <param name="user">Current claims principal.</param>
    /// <returns>Modified DtoOptions object.</returns>
    internal static DtoOptions AddClientFields(
        this DtoOptions dtoOptions, ClaimsPrincipal user)
    {
        dtoOptions.Fields ??= Array.Empty<ItemFields>();

        string? client = user.GetClient();

        // No client in claim
        if (string.IsNullOrEmpty(client))
        {
            return dtoOptions;
        }

        if (!dtoOptions.ContainsField(ItemFields.RecursiveItemCount))
        {
            if (client.Contains("kodi", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("wmc", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("media center", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("classic", StringComparison.OrdinalIgnoreCase))
            {
                dtoOptions.Fields = [..dtoOptions.Fields, ItemFields.RecursiveItemCount];
            }
        }

        if (!dtoOptions.ContainsField(ItemFields.ChildCount))
        {
            if (client.Contains("kodi", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("wmc", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("media center", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("classic", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("roku", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("samsung", StringComparison.OrdinalIgnoreCase) ||
                client.Contains("androidtv", StringComparison.OrdinalIgnoreCase))
            {
                dtoOptions.Fields = [..dtoOptions.Fields, ItemFields.ChildCount];
            }
        }

        return dtoOptions;
    }

    /// <summary>
    /// Add additional DtoOptions.
    /// </summary>
    /// <remarks>
    /// Converted from IHasDtoOptions.
    /// Legacy order: 3.
    /// </remarks>
    /// <param name="dtoOptions">DtoOptions object.</param>
    /// <param name="enableImages">Enable images.</param>
    /// <param name="enableUserData">Enable user data.</param>
    /// <param name="imageTypeLimit">Image type limit.</param>
    /// <param name="enableImageTypes">Enable image types.</param>
    /// <returns>Modified DtoOptions object.</returns>
    internal static DtoOptions AddAdditionalDtoOptions(
        this DtoOptions dtoOptions,
        bool? enableImages,
        bool? enableUserData,
        int? imageTypeLimit,
        IReadOnlyList<ImageType> enableImageTypes)
    {
        dtoOptions.EnableImages = enableImages ?? true;

        if (imageTypeLimit.HasValue)
        {
            dtoOptions.ImageTypeLimit = imageTypeLimit.Value;
        }

        if (enableUserData.HasValue)
        {
            dtoOptions.EnableUserData = enableUserData.Value;
        }

        if (enableImageTypes.Count != 0)
        {
            dtoOptions.ImageTypes = enableImageTypes;
        }

        return dtoOptions;
    }
}
