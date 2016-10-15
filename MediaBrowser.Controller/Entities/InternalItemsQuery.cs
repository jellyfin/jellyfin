using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Configuration;
using System.Linq;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Entities
{
    public class InternalItemsQuery
    {
        public bool Recursive { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }

        public string[] SortBy { get; set; }

        public SortOrder SortOrder { get; set; }

        public User User { get; set; }

        public BaseItem SimilarTo { get; set; }

        public bool? IsFolder { get; set; }
        public bool? IsFavorite { get; set; }
        public bool? IsFavoriteOrLiked { get; set; }
        public bool? IsLiked { get; set; }
        public bool? IsPlayed { get; set; }
        public bool? IsResumable { get; set; }
        public bool? IncludeItemsByName { get; set; }

        public string[] MediaTypes { get; set; }
        public string[] IncludeItemTypes { get; set; }
        public string[] ExcludeItemTypes { get; set; }
        public string[] ExcludeTags { get; set; }
        public string[] ExcludeInheritedTags { get; set; }
        public string[] Genres { get; set; }
        public string[] Keywords { get; set; }

        public bool? IsSpecialSeason { get; set; }
        public bool? IsMissing { get; set; }
        public bool? IsUnaired { get; set; }
        public bool? IsVirtualUnaired { get; set; }
        public bool? CollapseBoxSetItems { get; set; }

        public string NameStartsWithOrGreater { get; set; }
        public string NameStartsWith { get; set; }
        public string NameLessThan { get; set; }
        public string NameContains { get; set; }
        public string MinSortName { get; set; }

        public string PresentationUniqueKey { get; set; }
        public string Path { get; set; }
        public string PathNotStartsWith { get; set; }
        public string Name { get; set; }
        public string SlugName { get; set; }

        public string Person { get; set; }
        public string[] PersonIds { get; set; }
        public string[] ItemIds { get; set; }
        public string[] ExcludeItemIds { get; set; }
        public string AdjacentTo { get; set; }
        public string[] PersonTypes { get; set; }

        public bool? Is3D { get; set; }
        public bool? IsHD { get; set; }
        public bool? IsInBoxSet { get; set; }
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

        public string[] Studios { get; set; }
        public string[] StudioIds { get; set; }
        public string[] GenreIds { get; set; }
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

        public int? MinPlayers { get; set; }
        public int? MaxPlayers { get; set; }
        public int? MinIndexNumber { get; set; }
        public int? AiredDuringSeason { get; set; }
        public double? MinCriticRating { get; set; }
        public double? MinCommunityRating { get; set; }

        public string[] ChannelIds { get; set; }

        internal List<Guid> ItemIdsFromPersonFilters { get; set; }
        public int? ParentIndexNumber { get; set; }
        public int? ParentIndexNumberNotEquals { get; set; }
        public int? IndexNumber { get; set; }
        public int? MinParentalRating { get; set; }
        public int? MaxParentalRating { get; set; }

        public bool? IsCurrentSchema { get; set; }
        public bool? HasDeadParentId { get; set; }
        public bool? IsOffline { get; set; }
        public bool? IsVirtualItem { get; set; }

        public Guid? ParentId { get; set; }
        public string[] AncestorIds { get; set; }
        public string[] TopParentIds { get; set; }

        public LocationType[] LocationTypes { get; set; }
        public LocationType[] ExcludeLocationTypes { get; set; }
        public string[] PresetViews { get; set; }
        public SourceType[] SourceTypes { get; set; }
        public SourceType[] ExcludeSourceTypes { get; set; }
        public TrailerType[] TrailerTypes { get; set; }

        public DayOfWeek[] AirDays { get; set; }
        public SeriesStatus[] SeriesStatuses { get; set; }
        public string AlbumArtistStartsWithOrGreater { get; set; }
        public string ExternalSeriesId { get; set; }

        public string[] AlbumNames { get; set; }
        public string[] ArtistNames { get; set; }
        public string[] ExcludeArtistIds { get; set; }
        public string AncestorWithPresentationUniqueKey { get; set; }

        public bool GroupByPresentationUniqueKey { get; set; }
        public bool EnableTotalRecordCount { get; set; }
        public bool ForceDirect { get; set; }
        public Dictionary<string, string> ExcludeProviderIds { get; set; }
        public bool EnableGroupByMetadataKey { get; set; }

        public List<Tuple<string, SortOrder>> OrderBy { get; set; }

        public DateTime? MinDateCreated { get; set; }
        public DateTime? MinDateLastSaved { get; set; }

        public DtoOptions DtoOptions { get; set; }

        public bool HasField(ItemFields name)
        {
            var fields = DtoOptions.Fields;

            switch (name)
            {
                case ItemFields.ThemeSongIds:
                case ItemFields.ThemeVideoIds:
                case ItemFields.ProductionLocations:
                case ItemFields.Keywords:
                case ItemFields.Taglines:
                case ItemFields.ShortOverview:
                case ItemFields.CustomRating:
                case ItemFields.DateCreated:
                case ItemFields.SortName:
                case ItemFields.Overview:
                case ItemFields.OfficialRatingDescription:
                case ItemFields.HomePageUrl:
                case ItemFields.VoteCount:
                case ItemFields.DisplayMediaType:
                //case ItemFields.ServiceName:
                case ItemFields.Genres:
                case ItemFields.Studios:
                case ItemFields.Settings:
                case ItemFields.OriginalTitle:
                case ItemFields.Tags:
                case ItemFields.DateLastMediaAdded:
                case ItemFields.CriticRatingSummary:
                    return fields.Count == 0 || fields.Contains(name);
                default:
                    return true;
            }
        }

        public InternalItemsQuery()
        {
            GroupByPresentationUniqueKey = true;
            EnableTotalRecordCount = true;

            DtoOptions = new DtoOptions();
            AlbumNames = new string[] { };
            ArtistNames = new string[] { };
            ExcludeArtistIds = new string[] { };
            ExcludeProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            BlockUnratedItems = new UnratedItem[] { };
            Tags = new string[] { };
            OfficialRatings = new string[] { };
            SortBy = new string[] { };
            MediaTypes = new string[] { };
            Keywords = new string[] { };
            IncludeItemTypes = new string[] { };
            ExcludeItemTypes = new string[] { };
            Genres = new string[] { };
            Studios = new string[] { };
            StudioIds = new string[] { };
            GenreIds = new string[] { };
            ImageTypes = new ImageType[] { };
            VideoTypes = new VideoType[] { };
            Years = new int[] { };
            PersonTypes = new string[] { };
            PersonIds = new string[] { };
            ChannelIds = new string[] { };
            ItemIds = new string[] { };
            ExcludeItemIds = new string[] { };
            AncestorIds = new string[] { };
            TopParentIds = new string[] { };
            ExcludeTags = new string[] { };
            ExcludeInheritedTags = new string[] { };
            LocationTypes = new LocationType[] { };
            ExcludeLocationTypes = new LocationType[] { };
            PresetViews = new string[] { };
            SourceTypes = new SourceType[] { };
            ExcludeSourceTypes = new SourceType[] { };
            TrailerTypes = new TrailerType[] { };
            AirDays = new DayOfWeek[] { };
            SeriesStatuses = new SeriesStatus[] { };
            OrderBy = new List<Tuple<string, SortOrder>>();
        }

        public InternalItemsQuery(User user)
            : this()
        {
            SetUser(user);
        }

        public void SetUser(User user)
        {
            if (user != null)
            {
                var policy = user.Policy;
                MaxParentalRating = policy.MaxParentalRating;

                if (policy.MaxParentalRating.HasValue)
                {
                    BlockUnratedItems = policy.BlockUnratedItems;
                }

                ExcludeInheritedTags = policy.BlockedTags;

                User = user;
            }
        }
    }
}
