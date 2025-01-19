#pragma warning disable CA1044, CA1819, CA2227, CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public class InternalItemsQuery
    {
        public InternalItemsQuery()
        {
            AlbumArtistIds = Array.Empty<Guid>();
            AlbumIds = Array.Empty<Guid>();
            AncestorIds = Array.Empty<Guid>();
            ArtistIds = Array.Empty<Guid>();
            BlockUnratedItems = Array.Empty<UnratedItem>();
            BoxSetLibraryFolders = Array.Empty<Guid>();
            ChannelIds = Array.Empty<Guid>();
            ContributingArtistIds = Array.Empty<Guid>();
            DtoOptions = new DtoOptions();
            EnableTotalRecordCount = true;
            ExcludeArtistIds = Array.Empty<Guid>();
            ExcludeInheritedTags = Array.Empty<string>();
            IncludeInheritedTags = Array.Empty<string>();
            ExcludeItemIds = Array.Empty<Guid>();
            ExcludeItemTypes = Array.Empty<BaseItemKind>();
            ExcludeTags = Array.Empty<string>();
            GenreIds = Array.Empty<Guid>();
            Genres = Array.Empty<string>();
            GroupByPresentationUniqueKey = true;
            ImageTypes = Array.Empty<ImageType>();
            IncludeItemTypes = Array.Empty<BaseItemKind>();
            ItemIds = Array.Empty<Guid>();
            MediaTypes = Array.Empty<MediaType>();
            OfficialRatings = Array.Empty<string>();
            OrderBy = Array.Empty<(ItemSortBy, SortOrder)>();
            PersonIds = Array.Empty<Guid>();
            PersonTypes = Array.Empty<string>();
            PresetViews = Array.Empty<CollectionType?>();
            SeriesStatuses = Array.Empty<SeriesStatus>();
            SourceTypes = Array.Empty<SourceType>();
            StudioIds = Array.Empty<Guid>();
            Tags = Array.Empty<string>();
            TopParentIds = Array.Empty<Guid>();
            TrailerTypes = Array.Empty<TrailerType>();
            VideoTypes = Array.Empty<VideoType>();
            Years = Array.Empty<int>();
            SkipDeserialization = false;
        }

        public InternalItemsQuery(User? user)
            : this()
        {
            if (user is not null)
            {
                SetUser(user);
            }
        }

        public bool Recursive { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }

        public User? User { get; set; }

        public bool? IsFolder { get; set; }

        public bool? IsFavorite { get; set; }

        public bool? IsFavoriteOrLiked { get; set; }

        public bool? IsLiked { get; set; }

        public bool? IsPlayed { get; set; }

        public bool? IsResumable { get; set; }

        public bool? IncludeItemsByName { get; set; }

        public MediaType[] MediaTypes { get; set; }

        public BaseItemKind[] IncludeItemTypes { get; set; }

        public BaseItemKind[] ExcludeItemTypes { get; set; }

        public string[] ExcludeTags { get; set; }

        public string[] ExcludeInheritedTags { get; set; }

        public string[] IncludeInheritedTags { get; set; }

        public IReadOnlyList<string> Genres { get; set; }

        public bool? IsSpecialSeason { get; set; }

        public bool? IsMissing { get; set; }

        public bool? IsUnaired { get; set; }

        public bool? CollapseBoxSetItems { get; set; }

        public string? NameStartsWithOrGreater { get; set; }

        public string? NameStartsWith { get; set; }

        public string? NameLessThan { get; set; }

        public string? NameContains { get; set; }

        public string? MinSortName { get; set; }

        public string? PresentationUniqueKey { get; set; }

        public string? Path { get; set; }

        public string? Name { get; set; }

        public string? Person { get; set; }

        public Guid[] PersonIds { get; set; }

        public Guid[] ItemIds { get; set; }

        public Guid[] ExcludeItemIds { get; set; }

        public Guid? AdjacentTo { get; set; }

        public string[] PersonTypes { get; set; }

        public bool? Is3D { get; set; }

        public bool? IsHD { get; set; }

        public bool? IsLocked { get; set; }

        public bool? IsPlaceHolder { get; set; }

        public bool? HasImdbId { get; set; }

        public bool? HasOverview { get; set; }

        public bool? HasTmdbId { get; set; }

        public bool? HasOfficialRating { get; set; }

        public bool? HasTvdbId { get; set; }

        public bool? HasThemeSong { get; set; }

        public bool? HasThemeVideo { get; set; }

        public bool? HasSubtitles { get; set; }

        public bool? HasSpecialFeature { get; set; }

        public bool? HasTrailer { get; set; }

        public bool? HasParentalRating { get; set; }

        public Guid[] StudioIds { get; set; }

        public IReadOnlyList<Guid> GenreIds { get; set; }

        public ImageType[] ImageTypes { get; set; }

        public VideoType[] VideoTypes { get; set; }

        public UnratedItem[] BlockUnratedItems { get; set; }

        public int[] Years { get; set; }

        public string[] Tags { get; set; }

        public string[] OfficialRatings { get; set; }

        public DateTime? MinPremiereDate { get; set; }

        public DateTime? MaxPremiereDate { get; set; }

        public DateTime? MinStartDate { get; set; }

        public DateTime? MaxStartDate { get; set; }

        public DateTime? MinEndDate { get; set; }

        public DateTime? MaxEndDate { get; set; }

        public bool? IsAiring { get; set; }

        public bool? IsMovie { get; set; }

        public bool? IsSports { get; set; }

        public bool? IsKids { get; set; }

        public bool? IsNews { get; set; }

        public bool? IsSeries { get; set; }

        public int? MinIndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the minimum ParentIndexNumber and IndexNumber.
        /// </summary>
        /// <remarks>
        /// It produces this where clause:
        /// <para>(ParentIndexNumber = X and IndexNumber >= Y) or ParentIndexNumber > X.
        /// </para>
        /// </remarks>
        public (int ParentIndexNumber, int IndexNumber)? MinParentAndIndexNumber { get; set; }

        public int? AiredDuringSeason { get; set; }

        public double? MinCriticRating { get; set; }

        public double? MinCommunityRating { get; set; }

        public IReadOnlyList<Guid> ChannelIds { get; set; }

        public int? ParentIndexNumber { get; set; }

        public int? ParentIndexNumberNotEquals { get; set; }

        public int? IndexNumber { get; set; }

        public int? MinParentalRating { get; set; }

        public int? MaxParentalRating { get; set; }

        public bool? HasDeadParentId { get; set; }

        public bool? IsVirtualItem { get; set; }

        public Guid ParentId { get; set; }

        public BaseItemKind? ParentType { get; set; }

        public Guid[] AncestorIds { get; set; }

        public Guid[] TopParentIds { get; set; }

        public CollectionType?[] PresetViews { get; set; }

        public TrailerType[] TrailerTypes { get; set; }

        public SourceType[] SourceTypes { get; set; }

        public SeriesStatus[] SeriesStatuses { get; set; }

        public string? ExternalSeriesId { get; set; }

        public string? ExternalId { get; set; }

        public Guid[] AlbumIds { get; set; }

        public Guid[] ArtistIds { get; set; }

        public Guid[] ExcludeArtistIds { get; set; }

        public string? AncestorWithPresentationUniqueKey { get; set; }

        public string? SeriesPresentationUniqueKey { get; set; }

        public bool GroupByPresentationUniqueKey { get; set; }

        public bool GroupBySeriesPresentationUniqueKey { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        public bool ForceDirect { get; set; }

        public Dictionary<string, string>? ExcludeProviderIds { get; set; }

        public bool EnableGroupByMetadataKey { get; set; }

        public bool? HasChapterImages { get; set; }

        public IReadOnlyList<(ItemSortBy OrderBy, SortOrder SortOrder)> OrderBy { get; set; }

        public DateTime? MinDateCreated { get; set; }

        public DateTime? MinDateLastSaved { get; set; }

        public DateTime? MinDateLastSavedForUser { get; set; }

        public DtoOptions DtoOptions { get; set; }

        public string? HasNoAudioTrackWithLanguage { get; set; }

        public string? HasNoInternalSubtitleTrackWithLanguage { get; set; }

        public string? HasNoExternalSubtitleTrackWithLanguage { get; set; }

        public string? HasNoSubtitleTrackWithLanguage { get; set; }

        public bool? IsDeadArtist { get; set; }

        public bool? IsDeadStudio { get; set; }

        public bool? IsDeadPerson { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether album sub-folders should be returned if they exist.
        /// </summary>
        public bool? DisplayAlbumFolders { get; set; }

        public BaseItem? Parent
        {
            set
            {
                if (value is null)
                {
                    ParentId = Guid.Empty;
                    ParentType = null;
                }
                else
                {
                    ParentId = value.Id;
                    ParentType = value.GetBaseItemKind();
                }
            }
        }

        public Dictionary<string, string>? HasAnyProviderId { get; set; }

        public Guid[] AlbumArtistIds { get; set; }

        public Guid[] BoxSetLibraryFolders { get; set; }

        public Guid[] ContributingArtistIds { get; set; }

        public bool? HasAired { get; set; }

        public bool? HasOwnerId { get; set; }

        public bool? Is4K { get; set; }

        public int? MaxHeight { get; set; }

        public int? MaxWidth { get; set; }

        public int? MinHeight { get; set; }

        public int? MinWidth { get; set; }

        public string? SearchTerm { get; set; }

        public string? SeriesTimerId { get; set; }

        public bool SkipDeserialization { get; set; }

        public void SetUser(User user)
        {
            MaxParentalRating = user.MaxParentalAgeRating;

            if (MaxParentalRating.HasValue)
            {
                string other = UnratedItem.Other.ToString();
                BlockUnratedItems = user.GetPreference(PreferenceKind.BlockUnratedItems)
                    .Where(i => i != other)
                    .Select(e => Enum.Parse<UnratedItem>(e, true)).ToArray();
            }

            ExcludeInheritedTags = user.GetPreference(PreferenceKind.BlockedTags);
            IncludeInheritedTags = user.GetPreference(PreferenceKind.AllowedTags);

            User = user;
        }
    }
}
