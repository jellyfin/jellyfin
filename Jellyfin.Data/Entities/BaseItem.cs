using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class BaseItem
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

    public Guid Id { get; set; }

    public required string Type { get; set; }

    public IReadOnlyList<byte>? Data { get; set; }

    public Guid? ParentId { get; set; }

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

    public string? LockedFields { get; set; }

    public string? Studios { get; set; }

    public string? Audio { get; set; }

    public string? ExternalServiceId { get; set; }

    public string? Tags { get; set; }

    public bool IsFolder { get; set; }

    public int? InheritedParentalRatingValue { get; set; }

    public string? UnratedType { get; set; }

    public string? TopParentId { get; set; }

    public string? TrailerTypes { get; set; }

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

    public string? UserDataKey { get; set; }

    public string? SeasonName { get; set; }

    public Guid? SeasonId { get; set; }

    public Guid? SeriesId { get; set; }

    public string? ExternalSeriesId { get; set; }

    public string? Tagline { get; set; }

    public string? ProviderIds { get; set; }

    public string? Images { get; set; }

    public string? ProductionLocations { get; set; }

    public string? ExtraIds { get; set; }

    public int? TotalBitrate { get; set; }

    public string? ExtraType { get; set; }

    public string? Artists { get; set; }

    public string? AlbumArtists { get; set; }

    public string? ExternalId { get; set; }

    public string? SeriesPresentationUniqueKey { get; set; }

    public string? ShowId { get; set; }

    public string? OwnerId { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public long? Size { get; set; }
}
