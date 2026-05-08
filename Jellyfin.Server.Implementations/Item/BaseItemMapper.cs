#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;
using BaseItemEntity = Jellyfin.Database.Implementations.Entities.BaseItemEntity;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Handles mapping between BaseItemEntity (database) and BaseItemDto (domain) objects.
/// </summary>
internal static class BaseItemMapper
{
    /// <summary>
    /// This holds all the types in the running assemblies
    /// so that we can de-serialize properly when we don't have strong types.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type?> _typeMap = new ConcurrentDictionary<string, Type?>();

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="dto">The dto base instance.</param>
    /// <param name="appHost">The Application server Host.</param>
    /// <returns>The dto to map.</returns>
    public static BaseItemDto Map(BaseItemEntity entity, BaseItemDto dto, IServerApplicationHost? appHost)
    {
        dto.Id = entity.Id;
        dto.ParentId = entity.ParentId.GetValueOrDefault();
        dto.Path = appHost?.ExpandVirtualPath(entity.Path) ?? entity.Path;
        dto.EndDate = entity.EndDate;
        dto.CommunityRating = entity.CommunityRating;
        dto.CustomRating = entity.CustomRating;
        dto.IndexNumber = entity.IndexNumber;
        dto.IsLocked = entity.IsLocked;
        dto.Name = entity.Name;
        dto.OfficialRating = entity.OfficialRating;
        dto.Overview = entity.Overview;
        dto.ParentIndexNumber = entity.ParentIndexNumber;
        dto.PremiereDate = entity.PremiereDate;
        dto.ProductionYear = entity.ProductionYear;
        dto.SortName = entity.SortName;
        dto.ForcedSortName = entity.ForcedSortName;
        dto.RunTimeTicks = entity.RunTimeTicks;
        dto.PreferredMetadataLanguage = entity.PreferredMetadataLanguage;
        dto.PreferredMetadataCountryCode = entity.PreferredMetadataCountryCode;
        dto.IsInMixedFolder = entity.IsInMixedFolder;
        dto.InheritedParentalRatingValue = entity.InheritedParentalRatingValue;
        dto.InheritedParentalRatingSubValue = entity.InheritedParentalRatingSubValue;
        dto.CriticRating = entity.CriticRating;
        dto.PresentationUniqueKey = entity.PresentationUniqueKey;
        dto.OriginalTitle = entity.OriginalTitle;
        dto.OriginalLanguage = entity.OriginalLanguage;
        dto.Album = entity.Album;
        dto.LUFS = entity.LUFS;
        dto.NormalizationGain = entity.NormalizationGain;
        dto.IsVirtualItem = entity.IsVirtualItem;
        dto.ExternalSeriesId = entity.ExternalSeriesId;
        dto.Tagline = entity.Tagline;
        dto.TotalBitrate = entity.TotalBitrate;
        dto.ExternalId = entity.ExternalId;
        dto.Size = entity.Size;
        dto.Genres = string.IsNullOrWhiteSpace(entity.Genres) ? [] : entity.Genres.Split('|');
        dto.DateCreated = entity.DateCreated ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.DateModified = entity.DateModified ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.ChannelId = entity.ChannelId ?? Guid.Empty;
        dto.DateLastRefreshed = entity.DateLastRefreshed ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.DateLastSaved = entity.DateLastSaved ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.OwnerId = entity.OwnerId ?? Guid.Empty;
        dto.Width = entity.Width.GetValueOrDefault();
        dto.Height = entity.Height.GetValueOrDefault();
        dto.UserData = entity.UserData;

        if (entity.Provider is not null)
        {
            dto.ProviderIds = entity.Provider.ToDictionary(e => e.ProviderId, e => e.ProviderValue);
        }

        if (entity.ExtraType is not null)
        {
            dto.ExtraType = (ExtraType)entity.ExtraType;
        }

        if (entity.LockedFields is not null)
        {
            dto.LockedFields = entity.LockedFields?.Select(e => (MetadataField)e.Id).ToArray() ?? [];
        }

        if (entity.Audio is not null)
        {
            dto.Audio = (ProgramAudio)entity.Audio;
        }

        dto.ProductionLocations = entity.ProductionLocations?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        dto.Studios = entity.Studios?.Split('|') ?? [];
        dto.Tags = string.IsNullOrWhiteSpace(entity.Tags) ? [] : entity.Tags.Split('|');

        if (dto is IHasProgramAttributes hasProgramAttributes)
        {
            hasProgramAttributes.IsMovie = entity.IsMovie;
            hasProgramAttributes.IsSeries = entity.IsSeries;
            hasProgramAttributes.EpisodeTitle = entity.EpisodeTitle;
            hasProgramAttributes.IsRepeat = entity.IsRepeat;
        }

        if (dto is LiveTvChannel liveTvChannel)
        {
            liveTvChannel.ServiceName = entity.ExternalServiceId;
        }

        if (dto is Trailer trailer)
        {
            trailer.TrailerTypes = entity.TrailerTypes?.Select(e => (TrailerType)e.Id).ToArray() ?? [];
        }

        if (dto is Video video)
        {
            video.PrimaryVersionId = entity.PrimaryVersionId;
        }

        if (dto is IHasSeries hasSeriesName)
        {
            hasSeriesName.SeriesName = entity.SeriesName;
            hasSeriesName.SeriesId = entity.SeriesId.GetValueOrDefault();
            hasSeriesName.SeriesPresentationUniqueKey = entity.SeriesPresentationUniqueKey;
        }

        if (dto is Episode episode)
        {
            episode.SeasonName = entity.SeasonName;
            episode.SeasonId = entity.SeasonId.GetValueOrDefault();
        }

        if (dto is IHasArtist hasArtists)
        {
            hasArtists.Artists = entity.Artists?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            hasAlbumArtists.AlbumArtists = entity.AlbumArtists?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        if (dto is LiveTvProgram program)
        {
            program.ShowId = entity.ShowId;
        }

        if (entity.Images is not null)
        {
            dto.ImageInfos = entity.Images.Select(e => MapImageFromEntity(e, appHost)).ToArray();
        }

        if (dto is IHasStartDate hasStartDate)
        {
            hasStartDate.StartDate = entity.StartDate.GetValueOrDefault();
        }

        // Fields that are present in the DB but are never actually used
        // dto.UnratedType = entity.UnratedType;
        // dto.TopParentId = entity.TopParentId;
        // dto.CleanName = entity.CleanName;
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            folder.DateLastMediaAdded = entity.DateLastMediaAdded ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            if (entity.LinkedChildEntities is not null && entity.LinkedChildEntities.Count > 0)
            {
                folder.LinkedChildren = entity.LinkedChildEntities
                    .OrderBy(e => e.SortOrder)
                    .Select(e => new LinkedChild
                    {
                        ItemId = e.ChildId,
                        Type = (MediaBrowser.Controller.Entities.LinkedChildType)e.ChildType
                    })
                    .ToArray();
            }
        }

        return dto;
    }

    /// <summary>
    /// Maps a DTO to a database entity.
    /// </summary>
    /// <param name="dto">The DTO.</param>
    /// <param name="appHost">The application host for path resolution.</param>
    /// <returns>The database entity.</returns>
    public static BaseItemEntity Map(BaseItemDto dto, IServerApplicationHost appHost)
    {
        var dtoType = dto.GetType();
        var entity = new BaseItemEntity()
        {
            Type = dtoType.ToString(),
            Id = dto.Id
        };

        if (TypeRequiresDeserialization(dtoType))
        {
            entity.Data = JsonSerializer.Serialize(dto, dtoType, JsonDefaults.Options);
        }

        entity.ParentId = !dto.ParentId.IsEmpty() ? dto.ParentId : null;
        entity.Path = GetPathToSave(dto.Path, appHost);
        entity.EndDate = dto.EndDate;
        entity.CommunityRating = dto.CommunityRating;
        entity.CustomRating = dto.CustomRating;
        entity.IndexNumber = dto.IndexNumber;
        entity.IsLocked = dto.IsLocked;
        entity.Name = dto.Name;
        entity.CleanName = dto.Name.GetCleanValue();
        entity.OfficialRating = dto.OfficialRating;
        entity.Overview = dto.Overview;
        entity.ParentIndexNumber = dto.ParentIndexNumber;
        entity.PremiereDate = dto.PremiereDate;
        entity.ProductionYear = dto.ProductionYear;
        entity.SortName = dto.SortName;
        entity.ForcedSortName = dto.ForcedSortName;
        entity.RunTimeTicks = dto.RunTimeTicks;
        entity.PreferredMetadataLanguage = dto.PreferredMetadataLanguage;
        entity.PreferredMetadataCountryCode = dto.PreferredMetadataCountryCode;
        entity.IsInMixedFolder = dto.IsInMixedFolder;
        entity.InheritedParentalRatingValue = dto.InheritedParentalRatingValue;
        entity.InheritedParentalRatingSubValue = dto.InheritedParentalRatingSubValue;
        entity.CriticRating = dto.CriticRating;
        entity.PresentationUniqueKey = dto.PresentationUniqueKey;
        entity.OriginalTitle = dto.OriginalTitle;
        entity.OriginalLanguage = dto.OriginalLanguage;
        entity.Album = dto.Album;
        entity.LUFS = dto.LUFS;
        entity.NormalizationGain = dto.NormalizationGain;
        entity.IsVirtualItem = dto.IsVirtualItem;
        entity.ExternalSeriesId = dto.ExternalSeriesId;
        entity.Tagline = dto.Tagline;
        entity.TotalBitrate = dto.TotalBitrate;
        entity.ExternalId = dto.ExternalId;
        entity.Size = dto.Size;
        entity.Genres = string.Join('|', dto.Genres.Distinct(StringComparer.OrdinalIgnoreCase));
        entity.DateCreated = dto.DateCreated == DateTime.MinValue ? null : dto.DateCreated;
        entity.DateModified = dto.DateModified == DateTime.MinValue ? null : dto.DateModified;
        entity.ChannelId = dto.ChannelId;
        entity.DateLastRefreshed = dto.DateLastRefreshed == DateTime.MinValue ? null : dto.DateLastRefreshed;
        entity.DateLastSaved = dto.DateLastSaved == DateTime.MinValue ? null : dto.DateLastSaved;
        entity.OwnerId = dto.OwnerId == Guid.Empty ? null : dto.OwnerId;
        entity.Width = dto.Width;
        entity.Height = dto.Height;
        entity.Provider = dto.ProviderIds.Select(e => new BaseItemProvider()
        {
            Item = entity,
            ProviderId = e.Key,
            ProviderValue = e.Value
        }).ToList();

        if (dto.Audio.HasValue)
        {
            entity.Audio = (ProgramAudioEntity)dto.Audio;
        }

        if (dto.ExtraType.HasValue)
        {
            entity.ExtraType = (BaseItemExtraType)dto.ExtraType;
        }

        entity.ProductionLocations = dto.ProductionLocations is not null ? string.Join('|', dto.ProductionLocations.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase)) : null;
        entity.Studios = dto.Studios is not null ? string.Join('|', dto.Studios.Distinct(StringComparer.OrdinalIgnoreCase)) : null;
        entity.Tags = dto.Tags is not null ? string.Join('|', dto.Tags.Distinct(StringComparer.OrdinalIgnoreCase)) : null;
        entity.LockedFields = dto.LockedFields is not null ? dto.LockedFields
            .Select(e => new BaseItemMetadataField()
            {
                Id = (int)e,
                Item = entity,
                ItemId = entity.Id
            })
            .ToArray() : null;

        if (dto is IHasProgramAttributes hasProgramAttributes)
        {
            entity.IsMovie = hasProgramAttributes.IsMovie;
            entity.IsSeries = hasProgramAttributes.IsSeries;
            entity.EpisodeTitle = hasProgramAttributes.EpisodeTitle;
            entity.IsRepeat = hasProgramAttributes.IsRepeat;
        }

        if (dto is LiveTvChannel liveTvChannel)
        {
            entity.ExternalServiceId = liveTvChannel.ServiceName;
        }

        if (dto is Video video)
        {
            entity.PrimaryVersionId = video.PrimaryVersionId;
        }

        if (dto is IHasSeries hasSeriesName)
        {
            entity.SeriesName = hasSeriesName.SeriesName;
            entity.SeriesId = hasSeriesName.SeriesId;
            entity.SeriesPresentationUniqueKey = hasSeriesName.SeriesPresentationUniqueKey;
        }

        if (dto is Episode episode)
        {
            entity.SeasonName = episode.SeasonName;
            entity.SeasonId = episode.SeasonId;
        }

        if (dto is IHasArtist hasArtists)
        {
            entity.Artists = hasArtists.Artists is not null ? string.Join('|', hasArtists.Artists.Distinct(StringComparer.OrdinalIgnoreCase)) : null;
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            entity.AlbumArtists = hasAlbumArtists.AlbumArtists is not null ? string.Join('|', hasAlbumArtists.AlbumArtists.Distinct(StringComparer.OrdinalIgnoreCase)) : null;
        }

        if (dto is LiveTvProgram program)
        {
            entity.ShowId = program.ShowId;
        }

        if (dto.ImageInfos is not null)
        {
            entity.Images = dto.ImageInfos.Select(f => MapImageToEntity(dto.Id, f)).ToArray();
        }

        if (dto is Trailer trailer)
        {
            entity.TrailerTypes = trailer.TrailerTypes?.Select(e => new BaseItemTrailerType()
            {
                Id = (int)e,
                Item = entity,
                ItemId = entity.Id
            }).ToArray() ?? [];
        }

        entity.MediaType = dto.MediaType.ToString();
        if (dto is IHasStartDate hasStartDate)
        {
            entity.StartDate = hasStartDate.StartDate;
        }

        entity.UnratedType = dto.GetBlockUnratedType().ToString();

        // Fields that are present in the DB but are never actually used
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            entity.DateLastMediaAdded = folder.DateLastMediaAdded == DateTime.MinValue ? null : folder.DateLastMediaAdded;
            entity.IsFolder = folder.IsFolder;
        }

        return entity;
    }

    /// <summary>
    /// Maps a database image entity to a domain image info.
    /// </summary>
    /// <param name="e">The database image entity.</param>
    /// <param name="appHost">The application host.</param>
    /// <returns>The mapped image info.</returns>
    public static ItemImageInfo MapImageFromEntity(BaseItemImageInfo e, IServerApplicationHost? appHost)
    {
        return new ItemImageInfo()
        {
            Path = appHost?.ExpandVirtualPath(e.Path) ?? e.Path,
            BlurHash = e.Blurhash is null ? null : Encoding.UTF8.GetString(e.Blurhash),
            DateModified = e.DateModified ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            Height = e.Height,
            Width = e.Width,
            Type = (ImageType)e.ImageType
        };
    }

    /// <summary>
    /// Maps a domain image info to a database image entity.
    /// </summary>
    /// <param name="baseItemId">The parent item ID.</param>
    /// <param name="e">The image info to map.</param>
    /// <returns>The mapped database entity.</returns>
    public static BaseItemImageInfo MapImageToEntity(Guid baseItemId, ItemImageInfo e)
    {
        return new BaseItemImageInfo()
        {
            ItemId = baseItemId,
            Id = Guid.NewGuid(),
            Path = e.Path,
            Blurhash = e.BlurHash is null ? null : Encoding.UTF8.GetBytes(e.BlurHash),
            DateModified = e.DateModified,
            Height = e.Height,
            Width = e.Width,
            ImageType = (ImageInfoImageType)e.Type,
            Item = null!
        };
    }

    /// <summary>
    /// Gets the type from a type name string.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <returns>The resolved type, or null.</returns>
    public static Type? GetType(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return _typeMap.GetOrAdd(typeName, k => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(k))
            .FirstOrDefault(t => t is not null));
    }

    /// <summary>
    /// Checks whether a type requires JSON deserialization.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type requires deserialization.</returns>
    public static bool TypeRequiresDeserialization(Type type)
    {
        return type.GetCustomAttribute<RequiresSourceSerialisationAttribute>() == null;
    }

    /// <summary>
    /// Deserializes a BaseItemEntity and sets all properties.
    /// </summary>
    /// <param name="baseItemEntity">The DB entity.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="appHost">The application server Host.</param>
    /// <param name="skipDeserialization">If only mapping should be processed.</param>
    /// <returns>A mapped BaseItem, or null if the item type is unknown.</returns>
    public static BaseItemDto? DeserializeBaseItem(BaseItemEntity baseItemEntity, ILogger logger, IServerApplicationHost? appHost, bool skipDeserialization = false)
    {
        var type = GetType(baseItemEntity.Type);
        if (type is null)
        {
            logger.LogWarning(
                "Skipping item {ItemId} with unknown type '{ItemType}'. This may indicate a removed plugin or database corruption.",
                baseItemEntity.Id,
                baseItemEntity.Type);
            return null;
        }

        BaseItemDto? dto = null;
        if (TypeRequiresDeserialization(type) && baseItemEntity.Data is not null && !skipDeserialization)
        {
            try
            {
                dto = JsonSerializer.Deserialize(baseItemEntity.Data, type, JsonDefaults.Options) as BaseItemDto;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Error deserializing item with JSON: {Data}", baseItemEntity.Data);
            }
        }

        if (dto is null)
        {
            dto = Activator.CreateInstance(type) as BaseItemDto ?? throw new InvalidOperationException("Cannot deserialize unknown type.");
        }

        return Map(baseItemEntity, dto, appHost);
    }

    private static string? GetPathToSave(string path, IServerApplicationHost appHost)
    {
        if (path is null)
        {
            return null;
        }

        return appHost.ReverseVirtualPath(path);
    }
}
