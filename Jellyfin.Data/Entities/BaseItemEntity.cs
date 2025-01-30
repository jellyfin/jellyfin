#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA2227 // Collection properties should be read only

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

public class BaseItemEntity
{
    public required Guid Id { get; set; }

    public required string Type { get; set; }

    public string? Data { get; set; }

    public string? Path { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? ChannelId { get; set; }

    public bool IsMovie { get; set; }

    public float? CommunityRating { get; set; }

    public string? CustomRating { get; set; }

    public int? IndexNumber { get; set; }

    public bool IsLocked { get; set; }

    public string? Name { get; set; }

    public string? OfficialRating { get; set; }

    public string? MediaType { get; set; }

    public string? Overview { get; set; }

    public int? ParentIndexNumber { get; set; }

    public DateTime? PremiereDate { get; set; }

    public int? ProductionYear { get; set; }

    public string? Genres { get; set; }

    public string? SortName { get; set; }

    public string? ForcedSortName { get; set; }

    public long? RunTimeTicks { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public bool IsSeries { get; set; }

    public string? EpisodeTitle { get; set; }

    public bool IsRepeat { get; set; }

    public string? PreferredMetadataLanguage { get; set; }

    public string? PreferredMetadataCountryCode { get; set; }

    public DateTime? DateLastRefreshed { get; set; }

    public DateTime? DateLastSaved { get; set; }

    public bool IsInMixedFolder { get; set; }

    public string? Studios { get; set; }

    public string? ExternalServiceId { get; set; }

    public string? Tags { get; set; }

    public bool IsFolder { get; set; }

    public int? InheritedParentalRatingValue { get; set; }

    public string? UnratedType { get; set; }

    public float? CriticRating { get; set; }

    public string? CleanName { get; set; }

    public string? PresentationUniqueKey { get; set; }

    public string? OriginalTitle { get; set; }

    public string? PrimaryVersionId { get; set; }

    public DateTime? DateLastMediaAdded { get; set; }

    public string? Album { get; set; }

    public float? LUFS { get; set; }

    public float? NormalizationGain { get; set; }

    public bool IsVirtualItem { get; set; }

    public string? SeriesName { get; set; }

    public string? SeasonName { get; set; }

    public string? ExternalSeriesId { get; set; }

    public string? Tagline { get; set; }

    public string? ProductionLocations { get; set; }

    public string? ExtraIds { get; set; }

    public int? TotalBitrate { get; set; }

    public BaseItemExtraType? ExtraType { get; set; }

    public string? Artists { get; set; }

    public string? AlbumArtists { get; set; }

    public string? ExternalId { get; set; }

    public string? SeriesPresentationUniqueKey { get; set; }

    public string? ShowId { get; set; }

    public string? OwnerId { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public long? Size { get; set; }

    public ProgramAudioEntity? Audio { get; set; }

    public Guid? ParentId { get; set; }

    public Guid? TopParentId { get; set; }

    public Guid? SeasonId { get; set; }

    public Guid? SeriesId { get; set; }

    public ICollection<PeopleBaseItemMap>? Peoples { get; set; }

    public ICollection<UserData>? UserData { get; set; }

    public ICollection<ItemValueMap>? ItemValues { get; set; }

    public ICollection<MediaStreamInfo>? MediaStreams { get; set; }

    public ICollection<Chapter>? Chapters { get; set; }

    public ICollection<BaseItemProvider>? Provider { get; set; }

    public ICollection<AncestorId>? ParentAncestors { get; set; }

    public ICollection<AncestorId>? Children { get; set; }

    public ICollection<BaseItemMetadataField>? LockedFields { get; set; }

    public ICollection<BaseItemTrailerType>? TrailerTypes { get; set; }

    public ICollection<BaseItemImageInfo>? Images { get; set; }

    // those are references to __LOCAL__ ids not DB ids ... TODO: Bring the whole folder structure into the DB
    // public ICollection<BaseItemEntity>? SeriesEpisodes { get; set; }
    // public BaseItemEntity? Series { get; set; }
    // public BaseItemEntity? Season { get; set; }
    // public BaseItemEntity? Parent { get; set; }
    // public ICollection<BaseItemEntity>? DirectChildren { get; set; }
    // public BaseItemEntity? TopParent { get; set; }
    // public ICollection<BaseItemEntity>? AllChildren { get; set; }
    // public ICollection<BaseItemEntity>? SeasonEpisodes { get; set; }
}
