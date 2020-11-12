using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Api.Models.MediaInfoDtos
{
    /// <summary>
    /// Open live stream dto.
    /// </summary>
    public class OpenLiveStreamDto
    {
        /// <summary>
        /// Gets or sets the device profile.
        /// </summary>
        public DeviceProfile? DeviceProfile { get; set; }

        /// <summary>
        /// Gets or sets the device play protocols.
        /// </summary>
        public IReadOnlyList<MediaProtocol> DirectPlayProtocols { get; set; } = Array.Empty<MediaProtocol>();
    }
}
