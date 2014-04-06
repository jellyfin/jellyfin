
namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class PlaybackStartInfo.
    /// </summary>
    public class PlaybackStartInfo
    {
        public string UserId { get; set; }

        public string ItemId { get; set; }

        public string MediaSourceId { get; set; }

        public bool IsSeekable { get; set; }

        public string[] QueueableMediaTypes { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public PlaybackStartInfo()
        {
            QueueableMediaTypes = new string[] { };
        }
    }

    /// <summary>
    /// Class PlaybackProgressInfo.
    /// </summary>
    public class PlaybackProgressInfo
    {
        public string UserId { get; set; }

        public string ItemId { get; set; }

        public string MediaSourceId { get; set; }

        public long? PositionTicks { get; set; }

        public bool IsPaused { get; set; }

        public bool IsMuted { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? VolumeLevel { get; set; }
    }

    /// <summary>
    /// Class PlaybackStopInfo.
    /// </summary>
    public class PlaybackStopInfo
    {
        public string UserId { get; set; }

        public string ItemId { get; set; }

        public string MediaSourceId { get; set; }

        public long? PositionTicks { get; set; }
    }
}
