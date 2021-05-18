#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Data.Entities;

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
        }

        /// <summary>
        /// Gets or sets the maximum number of items the query should return.
        /// </summary>
        public int Limit { get; set; }

        public Guid ItemId { get; set; }

        public IReadOnlyList<string> PersonTypes { get; }

        public IReadOnlyList<string> ExcludePersonTypes { get; }

        public int? MaxListOrder { get; set; }

        public Guid AppearsInItemId { get; set; }

        public string NameContains { get; set; }

        public User User { get; set; }

        public bool? IsFavorite { get; set; }
    }
}
