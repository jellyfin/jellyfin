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

        public InternalItemsQuery()
        {
            SortBy = new string[] { };
            MediaTypes = new string[] { };
            IncludeItemTypes = new string[] { };
            ExcludeItemTypes = new string[] { };
        }
    }
}
