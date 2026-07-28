#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Entities
{
    public class InternalPeopleQuery
    {
        public InternalPeopleQuery()
         : this(Array.Empty<string>(), Array.Empty<string>())
        {
        }

        public InternalPeopleQuery(IReadOnlyList<string> personTypes, IReadOnlyList<string> excludePersonTypes)
        {
            PersonTypes = personTypes;
            ExcludePersonTypes = excludePersonTypes;
            EnableTotalRecordCount = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to count the matching people. Under an
        /// <see cref="AccessFilter"/> the count is the expensive half of the query: the page walk stops
        /// at the limit, the count has to check every person.
        /// </summary>
        public bool EnableTotalRecordCount { get; set; }

        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items the query should return.
        /// </summary>
        public int Limit { get; set; }

        public Guid ItemId { get; set; }

        public Guid? ParentId { get; set; }

        public IReadOnlyList<string> PersonTypes { get; }

        public IReadOnlyList<string> ExcludePersonTypes { get; }

        public int? MaxListOrder { get; set; }

        public Guid AppearsInItemId { get; set; }

        public string NameContains { get; set; }

        public string NameStartsWith { get; set; }

        public string NameLessThan { get; set; }

        public string NameStartsWithOrGreater { get; set; }

        public User User { get; set; }

        public bool? IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets the item query whose access settings (library access, parental rating, tags)
        /// people must satisfy through at least one of the items they are credited on.
        /// </summary>
        public InternalItemsQuery AccessFilter { get; set; }
    }
}
