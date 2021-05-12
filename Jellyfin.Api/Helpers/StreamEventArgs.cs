using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StreamingDtos;
using Jellyfin.Profiles;
using MediaBrowser.Controller.Devices;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Defines the <see cref="StreamEventType"/>.
    /// </summary>
    public enum StreamEventType
    {
        /// <summary>
        /// Triggered when the stream headers are being processed.
        /// </summary>
        OnHeaderProcessing = 0,

        /// <summary>
        /// Triggered just before the stream begins
        /// </summary>
        OnStreamStart = 1,

        /// <summary>
        /// Triggered when streaming properties are being processed.
        /// </summary>
        OnCodecProcessing = 3
    }

    /// <summary>
    /// Declares the <see cref="StreamEventArgs"/>.
    /// </summary>
    public class StreamEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating the type of event.
        /// </summary>
        public StreamEventType Type { get; set; } = StreamEventType.OnHeaderProcessing;

        /// <summary>
        /// Gets or sets a value indicating the streaming state.
        /// </summary>
        public StreamState? State { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the response headers.
        /// </summary>
        public IHeaderDictionary? ResponseHeaders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the stream is static.
        /// </summary>
        public bool IsStaticallyStreamed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the start time ticks.
        /// </summary>
        public long? StartTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the request.
        /// </summary>
        public HttpRequest? Request { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the stream request instance.
        /// </summary>
        public StreamingRequestDto? StreamingRequest { get; set; }

        /// <summary>
        /// Gets or sets a value device manager.
        /// </summary>
        public IDeviceManager? DeviceManager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the device profile id.
        /// </summary>
        public string? DeviceProfileId { get; set; }
    }
}
