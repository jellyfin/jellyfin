#nullable disable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// This is used by the api to get information about an Artist within a BaseItem.
    /// </summary>
    public class BaseItemArtist
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the primary image tag.
        /// </summary>
        /// <value>The primary image tag.</value>
        public string PrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the primary image blurhash.
        /// </summary>
        /// <value>The primary image blurhash.</value>
        public Dictionary<ImageType, Dictionary<string, string>> ImageBlurHashes { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has primary image.
        /// </summary>
        /// <value><c>true</c> if this instance has primary image; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool HasPrimaryImage => PrimaryImageTag is not null;
    }
}
