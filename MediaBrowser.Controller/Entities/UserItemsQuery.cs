using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Controller.Entities
{
    public class UserItemsQuery
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
        public bool? IsPlayed { get; set; }
        public bool? IsResumable { get; set; }

        public string[] MediaTypes { get; set; }
        
        public UserItemsQuery()
        {
            SortBy = new string[] { };
            MediaTypes = new string[] { };
        }
    }
}
