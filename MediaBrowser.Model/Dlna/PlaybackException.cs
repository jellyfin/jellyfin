using System;

namespace MediaBrowser.Model.Dlna
{
    public class PlaybackException : Exception
    {
        public PlaybackErrorCode ErrorCode { get; set;}
    }
}
