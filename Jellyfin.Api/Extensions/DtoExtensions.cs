using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Extensions;

/// <summary>
/// Dto Extensions.
/// </summary>
public static class DtoExtensions
{
    /// <summary>
    /// Gets the BaseItemKind values associated with the specified CollectionType.
    /// </summary>
    /// <param name="collectionType">The collection type to map to BaseItemKind values.</param>
    /// <returns>An array of BaseItemKind values that correspond to the collection type.</returns>
    public static BaseItemKind[] GetBaseItemKindsForCollectionType(CollectionType? collectionType)
    {
        switch (collectionType)
        {
            case CollectionType.movies:
                return [BaseItemKind.Movie];
            case CollectionType.tvshows:
                return [BaseItemKind.Series];
            case CollectionType.music:
                return [BaseItemKind.MusicAlbum];
            case CollectionType.musicvideos:
                return [BaseItemKind.MusicVideo];
            case CollectionType.books:
                return [BaseItemKind.Book, BaseItemKind.AudioBook];
            case CollectionType.boxsets:
                return [BaseItemKind.BoxSet];
            case CollectionType.homevideos:
            case CollectionType.photos:
                return [BaseItemKind.Video, BaseItemKind.Photo];
            case CollectionType.folders:
                return [];
            default:
                return [BaseItemKind.Video, BaseItemKind.Audio, BaseItemKind.Photo, BaseItemKind.Movie, BaseItemKind.Series];
        }
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

    /// <summary>
    /// Gets physical folder ids which are direct descendants of collection folders. These folders are TopParents for other items.
    /// </summary>
    /// <param name="dtoOptions">DtoOptions object.</param>
    /// <param name="user">User entity.</param>
    public static void AddOriginalTitlePreferenceDtoOptions(this DtoOptions dtoOptions, User user)
    {
        dtoOptions.OriginalTitlePreferenceLibraryIncludes = user.GetPreferenceValues<Guid>(PreferenceKind.OriginalTitlePreferenceFolderIncludes).ToHashSet();
        dtoOptions.OriginalTitlePreferenceLanguageScriptIncludes = user.GetPreferenceValues<string>(PreferenceKind.OriginalTitlePreferenceLanguageScriptIncludes);
        dtoOptions.OriginalTitlePreferenceLanguageScriptExcludes = user.GetPreferenceValues<string>(PreferenceKind.OriginalTitlePreferenceLanguageScriptExcludes);
    }
}
