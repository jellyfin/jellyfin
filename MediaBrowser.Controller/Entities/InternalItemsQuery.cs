using MediaBrowser.Model.Entities;
using System;

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

        public Func<BaseItem, User, bool> Filter { get; set; }

        public bool? IsFolder { get; set; }
        public bool? IsFavorite { get; set; }
        public bool? IsFavoriteOrLiked { get; set; }
        public bool? IsLiked { get; set; }
        public bool? IsPlayed { get; set; }
        public bool? IsResumable { get; set; }

        public string[] MediaTypes { get; set; }
        public string[] IncludeItemTypes { get; set; }
        public string[] ExcludeItemTypes { get; set; }
        public string[] Genres { get; set; }
        public string[] AllGenres { get; set; }

        public bool? IsMissing { get; set; }
        public bool? IsUnaired { get; set; }
        public bool? IsVirtualUnaired { get; set; }
        public bool? CollapseBoxSetItems { get; set; }

        public string NameStartsWithOrGreater { get; set; }
        public string NameStartsWith { get; set; }
        public string NameLessThan { get; set; }

        public string Person { get; set; }
        public string AdjacentTo { get; set; }
        public string[] PersonTypes { get; set; }

        public bool? Is3D { get; set; }
        public bool? IsHD { get; set; }
        public bool? IsInBoxSet { get; set; }
        public bool? IsLocked { get; set; }
        public bool? IsUnidentified { get; set; }
        public bool? IsPlaceHolder { get; set; }
        public bool? IsYearMismatched { get; set; }

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
        public ImageType[] ImageTypes { get; set; }
        public VideoType[] VideoTypes { get; set; }
        public int[] Years { get; set; }
        public string[] Tags { get; set; }
        public string[] OfficialRatings { get; set; }

        public InternalItemsQuery()
        {
            Tags = new string[] { };
            OfficialRatings = new string[] { };
            SortBy = new string[] { };
            MediaTypes = new string[] { };
            IncludeItemTypes = new string[] { };
            ExcludeItemTypes = new string[] { };
            AllGenres = new string[] { };
            Genres = new string[] { };
            Studios = new string[] { };
            ImageTypes = new ImageType[] { };
            VideoTypes = new VideoType[] { };
            Years = new int[] { };
            PersonTypes = new string[] { };
        }
    }
}
