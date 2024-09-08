using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.EntityFrameworkCore;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;
using BaseItemEntity = Jellyfin.Data.Entities.BaseItem;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Handles all storage logic for BaseItems.
/// </summary>
public class BaseItemManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _appHost;

     /// <summary>
    /// This holds all the types in the running assemblies
    /// so that we can de-serialize properly when we don't have strong types.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type?> _typeMap = new ConcurrentDictionary<string, Type?>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItemManager"/> class.
    /// </summary>
    /// <param name="dbProvider">The db factory.</param>
    public BaseItemManager(IDbContextFactory<JellyfinDbContext> dbProvider, IServerApplicationHost appHost)
    {
        _dbProvider = dbProvider;
        _appHost = appHost;
    }

    /// <summary>
    /// Gets the type.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <returns>Type.</returns>
    /// <exception cref="ArgumentNullException"><c>typeName</c> is null.</exception>
    private static Type? GetType(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return _typeMap.GetOrAdd(typeName, k => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(k))
            .FirstOrDefault(t => t is not null));
    }

    /// <summary>
    /// Saves the items.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> or <paramref name="cancellationToken"/> is <c>null</c>.
    /// </exception>
    public void UpdateOrInsertItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var itemsLen = items.Count;
        var tuples = new (BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, string? UserDataKey, List<string> InheritedTags)[itemsLen];
        for (int i = 0; i < itemsLen; i++)
        {
            var item = items[i];
            var ancestorIds = item.SupportsAncestors ?
                item.GetAncestorIds().Distinct().ToList() :
                null;

            var topParent = item.GetTopParent();

            var userdataKey = item.GetUserDataKeys().FirstOrDefault();
            var inheritedTags = item.GetInheritedTags();

            tuples[i] = (item, ancestorIds, topParent, userdataKey, inheritedTags);
        }

        using var context = _dbProvider.CreateDbContext();
        foreach (var item in tuples)
        {
            var entity = Map(item.Item);
            context.BaseItems.Add(entity);

            if (item.Item.SupportsAncestors && item.AncestorIds != null)
            {
                foreach (var ancestorId in item.AncestorIds)
                {
                    context.AncestorIds.Add(new Data.Entities.AncestorId()
                    {
                        Item = entity,
                        AncestorIdText = ancestorId.ToString(),
                        Id = ancestorId
                    });
                }
            }

            var itemValues = GetItemValuesToSave(item.Item, item.InheritedTags);
            context.ItemValues.Where(e => e.ItemId.Equals(entity.Id)).ExecuteDelete();
            foreach (var itemValue in itemValues)
            {
                context.ItemValues.Add(new()
                {
                    Item = entity,
                    Type = itemValue.MagicNumber,
                    Value = itemValue.Value,
                    CleanValue = GetCleanValue(itemValue.Value)
                });
            }
        }

        context.SaveChanges(true);
    }

    public BaseItemDto? GetSingle(Guid id)
    {
        if (id.IsEmpty())
        {
            throw new ArgumentException("Guid can't be empty", nameof(id));
        }

        using var context = _dbProvider.CreateDbContext();
        var item = context.BaseItems.FirstOrDefault(e => e.Id.Equals(id));
        if (item is null)
        {
            return null;
        }

        return DeserialiseBaseItem(item);
    }

    private BaseItemDto DeserialiseBaseItem(BaseItemEntity baseItemEntity)
    {
        var type = GetType(baseItemEntity.Type) ?? throw new InvalidOperationException("Cannot deserialise unkown type.");
        var dto = Activator.CreateInstance(type) as BaseItemDto ?? throw new InvalidOperationException("Cannot deserialise unkown type.");;
        return Map(baseItemEntity, dto);
    }

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="dto">The dto base instance.</param>
    /// <returns>The dto to map.</returns>
    public BaseItemDto Map(BaseItemEntity entity, BaseItemDto dto)
    {
        dto.Id = entity.Id;
        dto.ParentId = entity.ParentId.GetValueOrDefault();
        dto.Path = entity.Path;
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
        dto.CriticRating = entity.CriticRating;
        dto.PresentationUniqueKey = entity.PresentationUniqueKey;
        dto.OriginalTitle = entity.OriginalTitle;
        dto.Album = entity.Album;
        dto.LUFS = entity.LUFS;
        dto.NormalizationGain = entity.NormalizationGain;
        dto.IsVirtualItem = entity.IsVirtualItem;
        dto.ExternalSeriesId = entity.ExternalSeriesId;
        dto.Tagline = entity.Tagline;
        dto.TotalBitrate = entity.TotalBitrate;
        dto.ExternalId = entity.ExternalId;
        dto.Size = entity.Size;
        dto.Genres = entity.Genres?.Split('|');
        dto.DateCreated = entity.DateCreated.GetValueOrDefault();
        dto.DateModified = entity.DateModified.GetValueOrDefault();
        dto.ChannelId = string.IsNullOrWhiteSpace(entity.ChannelId) ? Guid.Empty : Guid.Parse(entity.ChannelId);
        dto.DateLastRefreshed = entity.DateLastRefreshed.GetValueOrDefault();
        dto.DateLastSaved = entity.DateLastSaved.GetValueOrDefault();
        dto.OwnerId = string.IsNullOrWhiteSpace(entity.OwnerId) ? Guid.Empty : Guid.Parse(entity.OwnerId);
        dto.Width = entity.Width.GetValueOrDefault();
        dto.Height = entity.Height.GetValueOrDefault();
        if (entity.ProviderIds is not null)
        {
            DeserializeProviderIds(entity.ProviderIds, dto);
        }

        if (entity.ExtraType is not null)
        {
            dto.ExtraType = Enum.Parse<ExtraType>(entity.ExtraType);
        }

        if (entity.LockedFields is not null)
        {
            List<MetadataField>? fields = null;
            foreach (var i in entity.LockedFields.AsSpan().Split('|'))
            {
                if (Enum.TryParse(i, true, out MetadataField parsedValue))
                {
                    (fields ??= new List<MetadataField>()).Add(parsedValue);
                }
            }

            dto.LockedFields = fields?.ToArray() ?? Array.Empty<MetadataField>();
        }

        if (entity.Audio is not null)
        {
            dto.Audio = Enum.Parse<ProgramAudio>(entity.Audio);
        }

        dto.ExtraIds = entity.ExtraIds?.Split('|').Select(e => Guid.Parse(e)).ToArray();
        dto.ProductionLocations = entity.ProductionLocations?.Split('|');
        dto.Studios = entity.Studios?.Split('|');
        dto.Tags = entity.Tags?.Split('|');

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
            List<TrailerType>? types = null;
            foreach (var i in entity.TrailerTypes.AsSpan().Split('|'))
            {
                if (Enum.TryParse(i, true, out TrailerType parsedValue))
                {
                    (types ??= new List<TrailerType>()).Add(parsedValue);
                }
            }

            trailer.TrailerTypes = types?.ToArray() ?? Array.Empty<TrailerType>();
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
            hasArtists.Artists = entity.Artists?.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            hasAlbumArtists.AlbumArtists = entity.AlbumArtists?.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }

        if (dto is LiveTvProgram program)
        {
            program.ShowId = entity.ShowId;
        }

        if (entity.Images is not null)
        {
            dto.ImageInfos = DeserializeImages(entity.Images);
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        // dto.MediaType = entity.MediaType;
        if (dto is IHasStartDate hasStartDate)
        {
            hasStartDate.StartDate = entity.StartDate;
        }

        // Fields that are present in the DB but are never actually used
        // dto.UnratedType = entity.UnratedType;
        // dto.TopParentId = entity.TopParentId;
        // dto.CleanName = entity.CleanName;
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            folder.DateLastMediaAdded = entity.DateLastMediaAdded;
        }

        return dto;
    }

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="dto">The entity.</param>
    /// <returns>The dto to map.</returns>
    public BaseItemEntity Map(BaseItemDto dto)
    {
        var entity = new BaseItemEntity()
        {
            Type = dto.GetType().ToString(),
        };
        entity.Id = dto.Id;
        entity.ParentId = dto.ParentId;
        entity.Path = GetPathToSave(dto.Path);
        entity.EndDate = dto.EndDate.GetValueOrDefault();
        entity.CommunityRating = dto.CommunityRating;
        entity.CustomRating = dto.CustomRating;
        entity.IndexNumber = dto.IndexNumber;
        entity.IsLocked = dto.IsLocked;
        entity.Name = dto.Name;
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
        entity.CriticRating = dto.CriticRating;
        entity.PresentationUniqueKey = dto.PresentationUniqueKey;
        entity.OriginalTitle = dto.OriginalTitle;
        entity.Album = dto.Album;
        entity.LUFS = dto.LUFS;
        entity.NormalizationGain = dto.NormalizationGain;
        entity.IsVirtualItem = dto.IsVirtualItem;
        entity.ExternalSeriesId = dto.ExternalSeriesId;
        entity.Tagline = dto.Tagline;
        entity.TotalBitrate = dto.TotalBitrate;
        entity.ExternalId = dto.ExternalId;
        entity.Size = dto.Size;
        entity.Genres = string.Join('|', dto.Genres);
        entity.DateCreated = dto.DateCreated;
        entity.DateModified = dto.DateModified;
        entity.ChannelId = dto.ChannelId.ToString();
        entity.DateLastRefreshed = dto.DateLastRefreshed;
        entity.DateLastSaved = dto.DateLastSaved;
        entity.OwnerId = dto.OwnerId.ToString();
        entity.Width = dto.Width;
        entity.Height = dto.Height;
        entity.ProviderIds = SerializeProviderIds(dto.ProviderIds);

        entity.Audio = dto.Audio?.ToString();
        entity.ExtraType = dto.ExtraType?.ToString();

        entity.ExtraIds = string.Join('|', dto.ExtraIds);
        entity.ProductionLocations = string.Join('|', dto.ProductionLocations);
        entity.Studios = dto.Studios is not null ? string.Join('|', dto.Studios) : null;
        entity.Tags = dto.Tags is not null ? string.Join('|', dto.Tags) : null;
        entity.LockedFields = dto.LockedFields is not null ? string.Join('|', dto.LockedFields) : null;

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

        if (dto is Trailer trailer)
        {
            entity.LockedFields = trailer.LockedFields is not null ? string.Join('|', trailer.LockedFields) : null;
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
            entity.Artists = hasArtists.Artists is not null ? string.Join('|', hasArtists.Artists) : null;
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            entity.AlbumArtists = hasAlbumArtists.AlbumArtists is not null ? string.Join('|', hasAlbumArtists.AlbumArtists) : null;
        }

        if (dto is LiveTvProgram program)
        {
            entity.ShowId = program.ShowId;
        }

        if (dto.ImageInfos is not null)
        {
            entity.Images = SerializeImages(dto.ImageInfos);
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        // dto.MediaType = entity.MediaType;
        if (dto is IHasStartDate hasStartDate)
        {
            entity.StartDate = hasStartDate.StartDate;
        }

        // Fields that are present in the DB but are never actually used
        // dto.UnratedType = entity.UnratedType;
        // dto.TopParentId = entity.TopParentId;
        // dto.CleanName = entity.CleanName;
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            entity.DateLastMediaAdded = folder.DateLastMediaAdded;
            entity.IsFolder = folder.IsFolder;
        }

        return entity;
    }

    private string GetCleanValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.RemoveDiacritics().ToLowerInvariant();
    }

    private List<(int MagicNumber, string Value)> GetItemValuesToSave(BaseItem item, List<string> inheritedTags)
    {
        var list = new List<(int, string)>();

        if (item is IHasArtist hasArtist)
        {
            list.AddRange(hasArtist.Artists.Select(i => (0, i)));
        }

        if (item is IHasAlbumArtist hasAlbumArtist)
        {
            list.AddRange(hasAlbumArtist.AlbumArtists.Select(i => (1, i)));
        }

        list.AddRange(item.Genres.Select(i => (2, i)));
        list.AddRange(item.Studios.Select(i => (3, i)));
        list.AddRange(item.Tags.Select(i => (4, i)));

        // keywords was 5

        list.AddRange(inheritedTags.Select(i => (6, i)));

        // Remove all invalid values.
        list.RemoveAll(i => string.IsNullOrWhiteSpace(i.Item2));

        return list;
    }

    internal static string? SerializeProviderIds(Dictionary<string, string> providerIds)
    {
        StringBuilder str = new StringBuilder();
        foreach (var i in providerIds)
        {
            // Ideally we shouldn't need this IsNullOrWhiteSpace check,
            // but we're seeing some cases of bad data slip through
            if (string.IsNullOrWhiteSpace(i.Value))
            {
                continue;
            }

            str.Append(i.Key)
                .Append('=')
                .Append(i.Value)
                .Append('|');
        }

        if (str.Length == 0)
        {
            return null;
        }

        str.Length -= 1; // Remove last |
        return str.ToString();
    }

    internal static void DeserializeProviderIds(string value, IHasProviderIds item)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var part in value.SpanSplit('|'))
        {
            var providerDelimiterIndex = part.IndexOf('=');
            // Don't let empty values through
            if (providerDelimiterIndex != -1 && part.Length != providerDelimiterIndex + 1)
            {
                item.SetProviderId(part[..providerDelimiterIndex].ToString(), part[(providerDelimiterIndex + 1)..].ToString());
            }
        }
    }

    internal string? SerializeImages(ItemImageInfo[] images)
    {
        if (images.Length == 0)
        {
            return null;
        }

        StringBuilder str = new StringBuilder();
        foreach (var i in images)
        {
            if (string.IsNullOrWhiteSpace(i.Path))
            {
                continue;
            }

            AppendItemImageInfo(str, i);
            str.Append('|');
        }

        str.Length -= 1; // Remove last |
        return str.ToString();
    }

    internal ItemImageInfo[] DeserializeImages(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<ItemImageInfo>();
        }

        // TODO The following is an ugly performance optimization, but it's extremely unlikely that the data in the database would be malformed
        var valueSpan = value.AsSpan();
        var count = valueSpan.Count('|') + 1;

        var position = 0;
        var result = new ItemImageInfo[count];
        foreach (var part in valueSpan.Split('|'))
        {
            var image = ItemImageInfoFromValueString(part);

            if (image is not null)
            {
                result[position++] = image;
            }
        }

        if (position == count)
        {
            return result;
        }

        if (position == 0)
        {
            return Array.Empty<ItemImageInfo>();
        }

        // Extremely unlikely, but somehow one or more of the image strings were malformed. Cut the array.
        return result[..position];
    }

    private void AppendItemImageInfo(StringBuilder bldr, ItemImageInfo image)
    {
        const char Delimiter = '*';

        var path = image.Path ?? string.Empty;

        bldr.Append(GetPathToSave(path))
            .Append(Delimiter)
            .Append(image.DateModified.Ticks)
            .Append(Delimiter)
            .Append(image.Type)
            .Append(Delimiter)
            .Append(image.Width)
            .Append(Delimiter)
            .Append(image.Height);

        var hash = image.BlurHash;
        if (!string.IsNullOrEmpty(hash))
        {
            bldr.Append(Delimiter)
                // Replace delimiters with other characters.
                // This can be removed when we migrate to a proper DB.
                .Append(hash.Replace(Delimiter, '/').Replace('|', '\\'));
        }
    }

    private string? GetPathToSave(string path)
    {
        if (path is null)
        {
            return null;
        }

        return _appHost.ReverseVirtualPath(path);
    }

    private string RestorePath(string path)
    {
        return _appHost.ExpandVirtualPath(path);
    }

    internal ItemImageInfo? ItemImageInfoFromValueString(ReadOnlySpan<char> value)
    {
        const char Delimiter = '*';

        var nextSegment = value.IndexOf(Delimiter);
        if (nextSegment == -1)
        {
            return null;
        }

        ReadOnlySpan<char> path = value[..nextSegment];
        value = value[(nextSegment + 1)..];
        nextSegment = value.IndexOf(Delimiter);
        if (nextSegment == -1)
        {
            return null;
        }

        ReadOnlySpan<char> dateModified = value[..nextSegment];
        value = value[(nextSegment + 1)..];
        nextSegment = value.IndexOf(Delimiter);
        if (nextSegment == -1)
        {
            nextSegment = value.Length;
        }

        ReadOnlySpan<char> imageType = value[..nextSegment];

        var image = new ItemImageInfo
        {
            Path = RestorePath(path.ToString())
        };

        if (long.TryParse(dateModified, CultureInfo.InvariantCulture, out var ticks)
            && ticks >= DateTime.MinValue.Ticks
            && ticks <= DateTime.MaxValue.Ticks)
        {
            image.DateModified = new DateTime(ticks, DateTimeKind.Utc);
        }
        else
        {
            return null;
        }

        if (Enum.TryParse(imageType, true, out ImageType type))
        {
            image.Type = type;
        }
        else
        {
            return null;
        }

        // Optional parameters: width*height*blurhash
        if (nextSegment + 1 < value.Length - 1)
        {
            value = value[(nextSegment + 1)..];
            nextSegment = value.IndexOf(Delimiter);
            if (nextSegment == -1 || nextSegment == value.Length)
            {
                return image;
            }

            ReadOnlySpan<char> widthSpan = value[..nextSegment];

            value = value[(nextSegment + 1)..];
            nextSegment = value.IndexOf(Delimiter);
            if (nextSegment == -1)
            {
                nextSegment = value.Length;
            }

            ReadOnlySpan<char> heightSpan = value[..nextSegment];

            if (int.TryParse(widthSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
                && int.TryParse(heightSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            {
                image.Width = width;
                image.Height = height;
            }

            if (nextSegment < value.Length - 1)
            {
                value = value[(nextSegment + 1)..];
                var length = value.Length;

                Span<char> blurHashSpan = stackalloc char[length];
                for (int i = 0; i < length; i++)
                {
                    var c = value[i];
                    blurHashSpan[i] = c switch
                    {
                        '/' => Delimiter,
                        '\\' => '|',
                        _ => c
                    };
                }

                image.BlurHash = new string(blurHashSpan);
            }
        }

        return image;
    }
}
