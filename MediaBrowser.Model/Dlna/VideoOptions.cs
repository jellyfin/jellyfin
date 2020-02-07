#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class VideoOptions.
    /// </summary>
    public class VideoOptions : AudioOptions
    {
        public int? AudioStreamIndex { get; set; }
        public int? SubtitleStreamIndex { get; set; }
    }
}
