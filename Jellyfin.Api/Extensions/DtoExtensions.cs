using System;
using System.Collections.Generic;
using System.Security.Claims;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Extensions
{
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
                if (client.IndexOf("kodi", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("wmc", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("media center", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("classic", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    int oldLen = dtoOptions.Fields.Count;
                    var arr = new ItemFields[oldLen + 1];
                    dtoOptions.Fields.CopyTo(arr, 0);
                    arr[oldLen] = ItemFields.RecursiveItemCount;
                    dtoOptions.Fields = arr;
                }
            }

            if (!dtoOptions.ContainsField(ItemFields.ChildCount))
            {
                if (client.IndexOf("kodi", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("wmc", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("media center", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("classic", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("roku", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("samsung", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("androidtv", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    int oldLen = dtoOptions.Fields.Count;
                    var arr = new ItemFields[oldLen + 1];
                    dtoOptions.Fields.CopyTo(arr, 0);
                    arr[oldLen] = ItemFields.ChildCount;
                    dtoOptions.Fields = arr;
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
}
