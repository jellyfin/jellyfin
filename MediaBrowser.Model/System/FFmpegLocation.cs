namespace MediaBrowser.Model.System
{
    /// <summary>
    /// Enum describing the location of the FFmpeg tool.
    /// </summary>
    public enum FFmpegLocation
    {
        /// <summary>No path to FFmpeg found.</summary>
        NotFound,

        /// <summary>Path supplied via command line using switch --ffmpeg.</summary>
        SetByArgument,

        /// <summary>User has supplied path via Transcoding UI page.</summary>
        Custom,

        /// <summary>FFmpeg tool found on system $PATH.</summary>
        System
    }
}