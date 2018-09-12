namespace MediaBrowser.Model.Dlna
{
    public enum SubtitleDeliveryMethod
    {
        /// <summary>
        /// Burn-in subtitles into the video.
        /// </summary>
        Encode = 0,
        /// <summary>
        /// Multiplex subtitles into the media stream.
        /// </summary>
        Embed = 1,
        /// <summary>
        /// Deliver subtitles as separate file or stream.
        /// </summary>
        External = 2,
        /// <summary>
        /// Deliver subtitles via HLS.
        /// </summary>
        Hls = 3
    }
}