#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Search
{
    public class SearchQuery
    {
        public SearchQuery()
        {
            IncludeArtists = true;
            IncludeGenres = true;
            IncludeMedia = true;
            IncludePeople = true;
            IncludeStudios = true;

            MediaTypes = Array.Empty<MediaType>();
            IncludeItemTypes = Array.Empty<BaseItemKind>();
            ExcludeItemTypes = Array.Empty<BaseItemKind>();
        }

        /// <summary>
        /// Gets or sets the user to localize search results for.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the search term.
        /// </summary>
        /// <value>The search term.</value>
        public required string SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets the start index. Used for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to return.
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        public bool IncludePeople { get; set; }

        public bool IncludeMedia { get; set; }

        public bool IncludeGenres { get; set; }

        public bool IncludeStudios { get; set; }

        public bool IncludeArtists { get; set; }

        public MediaType[] MediaTypes { get; set; }

        public BaseItemKind[] IncludeItemTypes { get; set; }

        public BaseItemKind[] ExcludeItemTypes { get; set; }

        public Guid? ParentId { get; set; }

        public bool? IsMovie { get; set; }

        public bool? IsSeries { get; set; }

        public bool? IsNews { get; set; }

        public bool? IsKids { get; set; }

        public bool? IsSports { get; set; }
    }
}
