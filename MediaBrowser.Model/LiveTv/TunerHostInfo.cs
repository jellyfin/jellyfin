#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.LiveTv
{
    public class TunerHostInfo
    {
        public TunerHostInfo()
        {
            AllowHWTranscoding = true;
            IgnoreDts = true;
            ReadAtNativeFramerate = false;
            AllowStreamSharing = true;
            AllowFmp4TranscodingContainer = false;
            FallbackMaxStreamingBitrate = 30000000;
            CustomHttpHeaders = Array.Empty<NameValuePair>();
        }

        public string Id { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }

        public string DeviceId { get; set; }

        public string FriendlyName { get; set; }

        public bool ImportFavoritesOnly { get; set; }

        public bool AllowHWTranscoding { get; set; }

        public bool AllowFmp4TranscodingContainer { get; set; }

        public bool AllowStreamSharing { get; set; }

        public int FallbackMaxStreamingBitrate { get; set; }

        public bool EnableStreamLooping { get; set; }

        public string Source { get; set; }

        public int TunerCount { get; set; }

        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the Referer header sent with HTTP requests for this tuner. Some IPTV
        /// providers reject requests (HTTP 406/403) unless a matching Referer is present.
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        /// Gets or sets additional custom HTTP headers sent with requests for this tuner.
        /// Applied on top of the User-Agent and Referer headers. Stored as name/value pairs
        /// so the value survives XML configuration serialization.
        /// </summary>
        public NameValuePair[] CustomHttpHeaders { get; set; }

        public bool IgnoreDts { get; set; }

        public bool ReadAtNativeFramerate { get; set; }
    }
}
