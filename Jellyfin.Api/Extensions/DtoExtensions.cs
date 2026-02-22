using System.Collections.Generic;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Extensions;

/// <summary>
/// Dto Extensions.
/// </summary>
public static class DtoExtensions
{
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
