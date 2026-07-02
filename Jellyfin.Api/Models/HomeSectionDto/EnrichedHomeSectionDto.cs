using System;
using System.Collections.Generic;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Api.Models.HomeSectionDto
{
    /// <summary>
    /// Home section dto with items.
    /// </summary>
    public class EnrichedHomeSectionDto
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the section options.
        /// </summary>
        public HomeSectionOptions SectionOptions { get; set; } = null!;

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        public IEnumerable<BaseItemDto> Items { get; set; } = Array.Empty<BaseItemDto>();
    }
}
