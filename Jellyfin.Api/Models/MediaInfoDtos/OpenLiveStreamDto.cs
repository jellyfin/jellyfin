using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("Microsoft.Performance", "CA1819:DontReturnArrays", MessageId = "DevicePlayProtocols", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "SA1011:ClosingBracketsSpace", MessageId = "DevicePlayProtocols", Justification = "Imported from ServiceStack")]
        public MediaProtocol[]? DirectPlayProtocols { get; set; }
    }
}
