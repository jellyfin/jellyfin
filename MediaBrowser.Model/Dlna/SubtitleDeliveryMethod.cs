namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the SubtitleDeliveryMethod.
    /// </summary>
    public enum SubtitleDeliveryMethod
    {
        /// <summary>
        /// Delivery method is encoded.
        /// </summary>
        Encode = 0,

        /// <summary>
        /// Delivery method is embedded.
        /// </summary>
        Embed = 1,

        /// <summary>
        /// Delivery method is external.
        /// </summary>
        External = 2,

        /// <summary>
        /// Delivery method is via HLS streaming.
        /// </summary>
        Hls = 3
    }
}
