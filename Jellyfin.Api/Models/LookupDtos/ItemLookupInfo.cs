#nullable disable

#pragma warning disable CA2227, CS1591

using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LookupDtos
{
    public class ItemLookupInfo
    {
        public ItemLookupInfo()
        {
#pragma warning disable CS0618 // The deprecated members are still carried until they are removed.
            IsAutomated = true;
#pragma warning restore CS0618
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the original title.
        /// </summary>
        /// <value>The original title of the item.</value>
        public string OriginalTitle { get; set; }

        /// <summary>
        /// Gets or sets the path. Deprecated, the server fills this in from the item being identified.
        /// </summary>
        /// <value>The path.</value>
        [Obsolete("Server side only. The path of the item on the server, no search provider reads it from a request.")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the metadata language.
        /// </summary>
        /// <value>The metadata language.</value>
        public string MetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the metadata country code.
        /// </summary>
        /// <value>The metadata country code.</value>
        public string MetadataCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        public Dictionary<string, string> ProviderIds { get; set; }

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        public int? Year { get; set; }

        public int? IndexNumber { get; set; }

        public int? ParentIndexNumber { get; set; }

        public DateTime? PremiereDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lookup was triggered by a scheduled refresh.
        /// Deprecated, a remote search is user initiated by definition.
        /// </summary>
        [Obsolete("Server side only. Set by the refresh pipeline and never read from a request.")]
        public bool IsAutomated { get; set; }
    }
}
