using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Api.Models.LiveTvDtos
{
    /// <summary>
    /// Channel mapping options dto.
    /// </summary>
    public class ChannelMappingOptionsDto
    {
        /// <summary>
        /// Gets or sets list of tuner channels.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA2227:ReadOnlyRemoveSetter", MessageId = "TunerChannels", Justification = "Imported from ServiceStack")]
        public List<TunerChannelMapping> TunerChannels { get; set; } = null!;

        /// <summary>
        /// Gets or sets list of provider channels.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA2227:ReadOnlyRemoveSetter", MessageId = "ProviderChannels", Justification = "Imported from ServiceStack")]
        public List<NameIdPair> ProviderChannels { get; set; } = null!;

        /// <summary>
        /// Gets or sets list of mappings.
        /// </summary>
        public IReadOnlyList<NameValuePair> Mappings { get; set; } = Array.Empty<NameValuePair>();

        /// <summary>
        /// Gets or sets provider name.
        /// </summary>
        public string? ProviderName { get; set; }
    }
}
