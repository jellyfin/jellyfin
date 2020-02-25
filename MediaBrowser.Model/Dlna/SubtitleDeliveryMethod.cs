#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Dlna
{
    public enum SubtitleDeliveryMethod
    {
        /// <summary>
        /// The encode
        /// </summary>
        Encode = 0,

        /// <summary>
        /// The embed
        /// </summary>
        Embed = 1,

        /// <summary>
        /// The external
        /// </summary>
        External = 2,
        
        /// <summary>
        /// The HLS
        /// </summary>
        Hls = 3
    }
}
